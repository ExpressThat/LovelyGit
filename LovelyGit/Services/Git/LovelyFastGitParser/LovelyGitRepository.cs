using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository : IDisposable
{
    private const int CommitCacheSize = 4096;
    private readonly GitObjectStore _objectStore;
    private readonly LruCache<GitObjectId, GitCommit> _commitCache = new(CommitCacheSize);
    private readonly Dictionary<string, GitRef> _refsByFullName;
    private readonly Dictionary<GitObjectId, List<string>> _branchNamesByCommit;
    private readonly Dictionary<GitObjectId, List<string>> _tagNamesByCommit;
    private readonly Dictionary<GitObjectId, List<GitCommitRef>> _refsByCommit;
    private readonly IReadOnlyList<string> _remotePrefixes;
    private bool _disposed;

    private LovelyGitRepository(
        string gitDirectory,
        string workTreeDirectory,
        GitObjectFormat objectFormat,
        GitObjectStore objectStore,
        GitObjectId? headTarget,
        string? currentBranchName,
        Dictionary<string, GitRef> refsByFullName,
        Dictionary<GitObjectId, List<string>> branchNamesByCommit,
        Dictionary<GitObjectId, List<string>> tagNamesByCommit,
        Dictionary<GitObjectId, List<GitCommitRef>> refsByCommit,
        IReadOnlyList<string> remotePrefixes)
    {
        GitDirectory = gitDirectory;
        WorkTreeDirectory = workTreeDirectory;
        ObjectFormat = objectFormat;
        _objectStore = objectStore;
        HeadTarget = headTarget;
        CurrentBranchName = currentBranchName;
        _refsByFullName = refsByFullName;
        _branchNamesByCommit = branchNamesByCommit;
        _tagNamesByCommit = tagNamesByCommit;
        _refsByCommit = refsByCommit;
        _remotePrefixes = remotePrefixes;
    }

    public string GitDirectory { get; }
    public string WorkTreeDirectory { get; }
    public GitObjectFormat ObjectFormat { get; }
    public GitObjectId? HeadTarget { get; }
    public string? CurrentBranchName { get; }
    public IReadOnlyList<string> RemotePrefixes => _remotePrefixes;

    public static async Task<LovelyGitRepository> OpenAsync(string path, CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(path, cancellationToken)
            .ConfigureAwait(false);
        var gitDirectory = paths.GitDirectory;
        var objectFormat = await GitRepositoryDiscovery.ReadObjectFormatAsync(gitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var objectStore = new GitObjectStore(gitDirectory, objectFormat);
        var rawRefs = await GitRefReader.LoadRefsAsync(gitDirectory, objectFormat, cancellationToken)
            .ConfigureAwait(false);
        var headTarget = await GitRefReader.ResolveHeadAsync(gitDirectory, objectFormat, rawRefs, cancellationToken)
            .ConfigureAwait(false);
        var currentBranchName = await GitRefReader.ResolveHeadBranchNameAsync(gitDirectory, cancellationToken)
            .ConfigureAwait(false);

        var refsByFullName = new Dictionary<string, GitRef>(StringComparer.Ordinal);
        var branchNamesByCommit = new Dictionary<GitObjectId, List<string>>();
        var tagNamesByCommit = new Dictionary<GitObjectId, List<string>>();
        var refsByCommit = new Dictionary<GitObjectId, List<GitCommitRef>>();
        var remotePrefixes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (fullName, rawRef) in rawRefs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var kind = GitRefReader.GetRefKind(fullName);
            var displayName = GitRefReader.GetDisplayRefName(fullName, kind);

            if (kind is GitRefKind.Head or GitRefKind.Remote)
            {
                refsByFullName[fullName] = new GitRef(displayName, rawRef.Target, kind);
                AddName(branchNamesByCommit, rawRef.Target, displayName);
                AddRef(refsByCommit, rawRef.Target, displayName, kind);
                if (kind == GitRefKind.Remote)
                {
                    AddRemotePrefix(remotePrefixes, displayName);
                }
            }
            else if (kind == GitRefKind.Tag)
            {
                var commitTarget = rawRef.PeeledTarget ?? rawRef.Target;
                refsByFullName[fullName] = new GitRef(displayName, commitTarget, kind);
                AddName(tagNamesByCommit, commitTarget, displayName);
                AddRef(refsByCommit, commitTarget, displayName, kind);
            }
        }

        return new LovelyGitRepository(
            gitDirectory,
            paths.WorkTreeDirectory,
            objectFormat,
            objectStore,
            headTarget,
            currentBranchName,
            refsByFullName,
            branchNamesByCommit,
            tagNamesByCommit,
            refsByCommit,
            remotePrefixes.Order(StringComparer.Ordinal).ToArray());
    }

    public async Task<GitCommit> GetCommitAsync(GitObjectId id, CancellationToken cancellationToken)
    {
        if (_commitCache.TryGet(id, out var cached))
        {
            return cached;
        }

        var data = await _objectStore.ReadObjectAsync(id, cancellationToken).ConfigureAwait(false);
        if (data.Kind != GitObjectKind.Commit)
        {
            throw new InvalidDataException($"Object is not a commit: {id}");
        }

        var commit = GitObjectParsers.ParseCommit(id, data.Data);
        if (_branchNamesByCommit.TryGetValue(id, out var branches))
        {
            commit.Branches.AddRange(branches);
        }

        if (_tagNamesByCommit.TryGetValue(id, out var tags))
        {
            commit.Tags.AddRange(tags);
        }

        if (_refsByCommit.TryGetValue(id, out var refs))
        {
            commit.Refs.AddRange(refs);
        }

        _commitCache.Set(id, commit);
        return commit;
    }

    public async Task<IReadOnlyList<GitCommit>> GetStartingCommitsAsync(CancellationToken cancellationToken)
    {
        var ids = new HashSet<GitObjectId>();
        foreach (var reference in _refsByFullName.Values)
        {
            if (reference.Kind != GitRefKind.Tag)
            {
                ids.Add(reference.Target);
            }
        }

        if (HeadTarget != null)
        {
            ids.Add(HeadTarget.Value);
        }

        var orderedIds = ids.ToArray();
        var commits = new GitCommit?[orderedIds.Length];
        await Parallel.ForEachAsync(
                Enumerable.Range(0, orderedIds.Length),
                cancellationToken,
                async (index, itemCancellationToken) =>
                {
                    itemCancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        commits[index] = await GetCommitAsync(orderedIds[index], itemCancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch when (!itemCancellationToken.IsCancellationRequested)
                    {
                        // Ignore refs that do not resolve to commits, matching the graph's previous behavior.
                    }
                })
            .ConfigureAwait(false);

        var resolvedCommits = new List<GitCommit>(commits.Length);
        foreach (var commit in commits)
        {
            if (commit != null)
            {
                resolvedCommits.Add(commit);
            }
        }

        return resolvedCommits;
    }

    public async Task<GitTreeComparison> GetChangedTreeFilesAsync(
        GitObjectId? parentTreeId,
        GitObjectId? currentTreeId,
        CancellationToken cancellationToken)
    {
        var parentFiles = new Dictionary<string, GitTreeFile>(StringComparer.Ordinal);
        var currentFiles = new Dictionary<string, GitTreeFile>(StringComparer.Ordinal);
        await CompareTreesAsync(parentTreeId, currentTreeId, string.Empty, parentFiles, currentFiles, cancellationToken)
            .ConfigureAwait(false);
        return new GitTreeComparison(parentFiles, currentFiles);
    }

    public async Task<byte[]> ReadBlobAsync(GitObjectId id, CancellationToken cancellationToken)
    {
        var data = await _objectStore.ReadObjectAsync(id, cancellationToken).ConfigureAwait(false);
        if (data.Kind != GitObjectKind.Blob)
        {
            throw new InvalidDataException($"Object is not a blob: {id}");
        }

        return data.Data;
    }

    public IReadOnlyList<GitRef> GetBranches()
    {
        return _refsByFullName.Values
            .Where(reference => reference.Kind is GitRefKind.Head or GitRefKind.Remote)
            .OrderBy(reference => reference.Kind)
            .ThenBy(reference => reference.Name, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<GitRef> GetTags()
    {
        return _refsByFullName.Values
            .Where(reference => reference.Kind == GitRefKind.Tag)
            .OrderBy(reference => reference.Name, StringComparer.Ordinal)
            .ToList();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _commitCache.Clear();
        _refsByFullName.Clear();
        _branchNamesByCommit.Clear();
        _tagNamesByCommit.Clear();
        _refsByCommit.Clear();
        _objectStore.Dispose();
        _disposed = true;
    }

}
