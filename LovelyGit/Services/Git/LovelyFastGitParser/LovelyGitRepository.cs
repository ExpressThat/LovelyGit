using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed class LovelyGitRepository : IDisposable
{
    private const int CommitCacheSize = 4096;
    private readonly GitObjectStore _objectStore;
    private readonly LruCache<GitObjectId, GitCommit> _commitCache = new(CommitCacheSize);
    private readonly Dictionary<string, GitRef> _refsByFullName;
    private readonly Dictionary<GitObjectId, List<string>> _branchNamesByCommit;
    private readonly Dictionary<GitObjectId, List<string>> _tagNamesByCommit;

    private LovelyGitRepository(
        string gitDirectory,
        GitObjectFormat objectFormat,
        GitObjectStore objectStore,
        GitObjectId? headTarget,
        Dictionary<string, GitRef> refsByFullName,
        Dictionary<GitObjectId, List<string>> branchNamesByCommit,
        Dictionary<GitObjectId, List<string>> tagNamesByCommit)
    {
        GitDirectory = gitDirectory;
        ObjectFormat = objectFormat;
        _objectStore = objectStore;
        HeadTarget = headTarget;
        _refsByFullName = refsByFullName;
        _branchNamesByCommit = branchNamesByCommit;
        _tagNamesByCommit = tagNamesByCommit;
    }

    public string GitDirectory { get; }
    public GitObjectFormat ObjectFormat { get; }
    public GitObjectId? HeadTarget { get; }

    public static async Task<LovelyGitRepository> OpenAsync(string path, CancellationToken cancellationToken)
    {
        var gitDirectory = await GitRepositoryDiscovery.ResolveGitDirectoryAsync(path, cancellationToken)
            .ConfigureAwait(false);
        var objectFormat = await GitRepositoryDiscovery.ReadObjectFormatAsync(gitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var objectStore = new GitObjectStore(gitDirectory, objectFormat);
        var rawRefs = await GitRefReader.LoadRefsAsync(gitDirectory, objectFormat, cancellationToken)
            .ConfigureAwait(false);
        var headTarget = await GitRefReader.ResolveHeadAsync(gitDirectory, objectFormat, rawRefs, cancellationToken)
            .ConfigureAwait(false);

        var refsByFullName = new Dictionary<string, GitRef>(StringComparer.Ordinal);
        var branchNamesByCommit = new Dictionary<GitObjectId, List<string>>();
        var tagNamesByCommit = new Dictionary<GitObjectId, List<string>>();

        foreach (var (fullName, rawRef) in rawRefs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var kind = GitRefReader.GetRefKind(fullName);
            var displayName = GitRefReader.GetDisplayRefName(fullName, kind);

            if (kind is GitRefKind.Head or GitRefKind.Remote)
            {
                refsByFullName[fullName] = new GitRef(displayName, rawRef.Target, kind);
                AddName(branchNamesByCommit, rawRef.Target, displayName);
            }
            else if (kind == GitRefKind.Tag)
            {
                var commitTarget = rawRef.PeeledTarget ?? rawRef.Target;
                refsByFullName[fullName] = new GitRef(displayName, commitTarget, kind);
                AddName(tagNamesByCommit, commitTarget, displayName);
            }
        }

        return new LovelyGitRepository(
            gitDirectory,
            objectFormat,
            objectStore,
            headTarget,
            refsByFullName,
            branchNamesByCommit,
            tagNamesByCommit);
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
    }

    private async Task ReadTreeFilesAsync(
        GitObjectId treeId,
        string prefix,
        Dictionary<string, GitTreeFile> files,
        CancellationToken cancellationToken)
    {
        foreach (var entry in await ReadTreeEntriesAsync(treeId, prefix, cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.IsTree)
            {
                await ReadTreeFilesAsync(entry.ObjectId, entry.Path, files, cancellationToken).ConfigureAwait(false);
                continue;
            }

            files[entry.Path] = new GitTreeFile(entry.Path, entry.ObjectId, entry.Mode);
        }
    }

    private async Task CompareTreesAsync(
        GitObjectId? parentTreeId,
        GitObjectId? currentTreeId,
        string prefix,
        Dictionary<string, GitTreeFile> parentFiles,
        Dictionary<string, GitTreeFile> currentFiles,
        CancellationToken cancellationToken)
    {
        if (parentTreeId == currentTreeId)
        {
            return;
        }

        var parentEntries = parentTreeId == null
            ? new Dictionary<string, GitTreeEntry>(StringComparer.Ordinal)
            : (await ReadTreeEntriesAsync(parentTreeId.Value, prefix, cancellationToken).ConfigureAwait(false))
                .ToDictionary(entry => entry.Name, StringComparer.Ordinal);
        var currentEntries = currentTreeId == null
            ? new Dictionary<string, GitTreeEntry>(StringComparer.Ordinal)
            : (await ReadTreeEntriesAsync(currentTreeId.Value, prefix, cancellationToken).ConfigureAwait(false))
                .ToDictionary(entry => entry.Name, StringComparer.Ordinal);
        var names = parentEntries.Keys
            .Concat(currentEntries.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal);

        foreach (var name in names)
        {
            cancellationToken.ThrowIfCancellationRequested();
            parentEntries.TryGetValue(name, out var parentEntry);
            currentEntries.TryGetValue(name, out var currentEntry);

            if (parentEntry?.ObjectId == currentEntry?.ObjectId && parentEntry?.Mode == currentEntry?.Mode)
            {
                continue;
            }

            if (parentEntry?.IsTree == true && currentEntry?.IsTree == true)
            {
                await CompareTreesAsync(
                        parentEntry.ObjectId,
                        currentEntry.ObjectId,
                        parentEntry.Path,
                        parentFiles,
                        currentFiles,
                        cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            if (parentEntry != null)
            {
                await CollectTreeEntryFilesAsync(parentEntry, parentFiles, cancellationToken).ConfigureAwait(false);
            }

            if (currentEntry != null)
            {
                await CollectTreeEntryFilesAsync(currentEntry, currentFiles, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task CollectTreeEntryFilesAsync(
        GitTreeEntry entry,
        Dictionary<string, GitTreeFile> files,
        CancellationToken cancellationToken)
    {
        if (!entry.IsTree)
        {
            files[entry.Path] = new GitTreeFile(entry.Path, entry.ObjectId, entry.Mode);
            return;
        }

        await ReadTreeFilesAsync(entry.ObjectId, entry.Path, files, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<GitTreeEntry>> ReadTreeEntriesAsync(
        GitObjectId treeId,
        string prefix,
        CancellationToken cancellationToken)
    {
        var data = await _objectStore.ReadObjectAsync(treeId, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return GitObjectParsers.ParseTreeEntries(treeId, ObjectFormat, data, prefix);
    }

    private static void AddName(Dictionary<GitObjectId, List<string>> map, GitObjectId id, string name)
    {
        if (!map.TryGetValue(id, out var names))
        {
            names = new List<string>();
            map[id] = names;
        }

        if (!names.Contains(name, StringComparer.Ordinal))
        {
            names.Add(name);
        }
    }
}
