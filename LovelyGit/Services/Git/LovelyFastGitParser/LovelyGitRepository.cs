using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository : IDisposable
{
    private const int CommitCacheSize = 256;
    private const int GraphCommitCacheSize = 1024;
    private const int GraphHeaderCacheSize = 2048;
    private readonly GitObjectStore _objectStore;
    private readonly LruCache<GitObjectId, GitCommit> _commitCache = new(CommitCacheSize);
    private readonly LruCache<GitObjectId, GitCommit> _graphCommitCache = new(GraphCommitCacheSize);
    private readonly LruCache<GitObjectId, GitCommit> _graphHeaderCache = new(GraphHeaderCacheSize);
    private readonly Dictionary<string, GitRef> _refsByFullName;
    private readonly Dictionary<GitObjectId, List<GitCommitRef>> _refsByCommit;
    private readonly IReadOnlyList<string> _remotePrefixes;
    private bool _disposed;

    private LovelyGitRepository(
        string gitDirectory,
        string worktreeGitDirectory,
        string workTreeDirectory,
        GitObjectFormat objectFormat,
        GitObjectStore objectStore,
        GitObjectId? headTarget,
        string? currentBranchName,
        Dictionary<string, GitRef> refsByFullName,
        Dictionary<GitObjectId, List<GitCommitRef>> refsByCommit,
        IReadOnlyList<string> remotePrefixes)
    {
        GitDirectory = gitDirectory;
        WorktreeGitDirectory = worktreeGitDirectory;
        WorkTreeDirectory = workTreeDirectory;
        ObjectFormat = objectFormat;
        _objectStore = objectStore;
        HeadTarget = headTarget;
        CurrentBranchName = currentBranchName;
        _refsByFullName = refsByFullName;
        _refsByCommit = refsByCommit;
        _remotePrefixes = remotePrefixes;
    }

    public string GitDirectory { get; }
    public string WorktreeGitDirectory { get; }
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
        var rawRefs = await GitRefReader.LoadRefsAsync(
                gitDirectory,
                objectFormat,
                GitRefReader.DefaultTagLimit,
                cancellationToken)
            .ConfigureAwait(false);
        var headTarget = await GitRefReader.ResolveHeadAsync(
                paths.WorktreeGitDirectory, objectFormat, rawRefs, cancellationToken)
            .ConfigureAwait(false);
        var currentBranchName = await GitRefReader
            .ResolveHeadBranchNameAsync(paths.WorktreeGitDirectory, cancellationToken)
            .ConfigureAwait(false);

        var refsByFullName = new Dictionary<string, GitRef>(StringComparer.Ordinal);
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
                AddRef(refsByCommit, rawRef.Target, displayName, kind);
                if (kind == GitRefKind.Remote)
                {
                    AddRemotePrefix(remotePrefixes, displayName);
                }
            }
            else if (kind == GitRefKind.Tag)
            {
                var commitTarget = rawRef.PeeledTarget ?? (rawRef.RequiresPeeling
                    ? await ResolveTagCommitTargetAsync(
                            objectStore,
                            objectFormat,
                            rawRef.Target,
                            cancellationToken)
                        .ConfigureAwait(false)
                    : rawRef.Target);
                refsByFullName[fullName] = new GitRef(
                    displayName,
                    commitTarget ?? rawRef.Target,
                    kind);
                if (commitTarget is not null)
                {
                    AddRef(refsByCommit, commitTarget.Value, displayName, kind);
                }
            }
            else if (kind == GitRefKind.Stash)
            {
                refsByFullName[fullName] = new GitRef(displayName, rawRef.Target, kind);
                AddRef(refsByCommit, rawRef.Target, displayName, kind);
            }
        }

        return new LovelyGitRepository(
            gitDirectory,
            paths.WorktreeGitDirectory,
            paths.WorkTreeDirectory,
            objectFormat,
            objectStore,
            headTarget,
            currentBranchName,
            refsByFullName,
            refsByCommit,
            remotePrefixes.Order(StringComparer.Ordinal).ToArray());
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

    internal async ValueTask<byte[]> ReadBlobWithoutCachingAsync(
        GitObjectId id,
        CancellationToken cancellationToken)
    {
        var data = await _objectStore
            .ReadObjectWithoutCachingAsync(id, cancellationToken)
            .ConfigureAwait(false);
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

    public bool TryGetBranch(string displayName, out GitRef? reference)
    {
        if (_refsByFullName.TryGetValue($"refs/heads/{displayName}", out reference))
        {
            return true;
        }

        return _refsByFullName.TryGetValue($"refs/remotes/{displayName}", out reference);
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
        _graphCommitCache.Clear();
        _graphHeaderCache.Clear();
        _refsByFullName.Clear();
        _refsByCommit.Clear();
        _objectStore.Dispose();
        _disposed = true;
    }

}
