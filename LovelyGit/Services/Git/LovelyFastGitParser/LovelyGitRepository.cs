using System.Text;

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
        var gitDirectory = await ResolveGitDirectoryAsync(path, cancellationToken).ConfigureAwait(false);
        var objectFormat = await ReadObjectFormatAsync(gitDirectory, cancellationToken).ConfigureAwait(false);
        var objectStore = new GitObjectStore(gitDirectory, objectFormat);
        var rawRefs = await LoadRefsAsync(gitDirectory, objectFormat, cancellationToken).ConfigureAwait(false);
        var headTarget = await ResolveHeadAsync(gitDirectory, objectFormat, rawRefs, cancellationToken).ConfigureAwait(false);

        var refsByFullName = new Dictionary<string, GitRef>(StringComparer.Ordinal);
        var branchNamesByCommit = new Dictionary<GitObjectId, List<string>>();
        var tagNamesByCommit = new Dictionary<GitObjectId, List<string>>();

        foreach (var (fullName, rawRef) in rawRefs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var kind = GetRefKind(fullName);
            var displayName = GetDisplayRefName(fullName, kind);

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

        var commit = ParseCommit(id, data.Data);
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

        var commits = new List<GitCommit>(ids.Count);
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                commits.Add(await GetCommitAsync(id, cancellationToken).ConfigureAwait(false));
            }
            catch when (!cancellationToken.IsCancellationRequested)
            {
                // Ignore refs that do not resolve to commits, matching the graph's previous behavior.
            }
        }

        return commits;
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

    private static async Task<string> ResolveGitDirectoryAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        var attributes = File.GetAttributes(fullPath);
        if ((attributes & FileAttributes.Directory) == 0)
        {
            throw new DirectoryNotFoundException($"Path is not a directory: {path}");
        }

        if (Path.GetFileName(fullPath).Equals(".git", StringComparison.OrdinalIgnoreCase))
        {
            return fullPath;
        }

        var dotGitPath = Path.Combine(fullPath, ".git");
        if (Directory.Exists(dotGitPath))
        {
            return dotGitPath;
        }

        if (File.Exists(dotGitPath))
        {
            var text = (await File.ReadAllTextAsync(dotGitPath, cancellationToken).ConfigureAwait(false)).Trim();
            const string prefix = "gitdir:";
            if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(".git file does not contain a gitdir pointer.");
            }

            var gitDir = text[prefix.Length..].Trim();
            return Path.GetFullPath(Path.IsPathRooted(gitDir) ? gitDir : Path.Combine(fullPath, gitDir));
        }

        throw new DirectoryNotFoundException($"Could not find .git directory for: {path}");
    }

    private static async Task<GitObjectFormat> ReadObjectFormatAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(gitDirectory, "config");
        if (!File.Exists(configPath))
        {
            return GitObjectFormat.Sha1;
        }

        var section = string.Empty;
        foreach (var rawLine in await File.ReadAllLinesAsync(configPath, cancellationToken).ConfigureAwait(false))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
            {
                continue;
            }

            if (line[0] == '[' && line[^1] == ']')
            {
                section = line[1..^1].Trim().Trim('"').ToLowerInvariant();
                continue;
            }

            if (!section.Equals("extensions", StringComparison.Ordinal) ||
                !TryReadConfigKeyValue(line, out var key, out var value) ||
                !key.Equals("objectformat", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return value.ToLowerInvariant() switch
            {
                "sha1" => GitObjectFormat.Sha1,
                "sha256" => GitObjectFormat.Sha256,
                _ => throw new NotSupportedException($"Unsupported Git object format: {value}"),
            };
        }

        return GitObjectFormat.Sha1;
    }

    private static bool TryReadConfigKeyValue(string line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        var separator = line.IndexOf('=');
        if (separator <= 0)
        {
            return false;
        }

        key = line[..separator].Trim();
        value = line[(separator + 1)..].Trim().Trim('"');
        return key.Length > 0;
    }

    private static async Task<Dictionary<string, RawRef>> LoadRefsAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var refs = new Dictionary<string, RawRef>(StringComparer.Ordinal);
        var refsDirectory = Path.Combine(gitDirectory, "refs");
        if (Directory.Exists(refsDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(refsDirectory, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var text = (await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false)).Trim();
                if (GitObjectId.TryParse(text, objectFormat, out var id))
                {
                    refs[Path.GetRelativePath(gitDirectory, file).Replace('\\', '/')] = new RawRef(id, null);
                }
            }
        }

        var packedRefsPath = Path.Combine(gitDirectory, "packed-refs");
        if (File.Exists(packedRefsPath))
        {
            string? lastRefName = null;
            foreach (var rawLine in await File.ReadAllLinesAsync(packedRefsPath, cancellationToken).ConfigureAwait(false))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                if (line[0] == '^')
                {
                    if (lastRefName != null && GitObjectId.TryParse(line[1..], objectFormat, out var peeled))
                    {
                        var existing = refs[lastRefName];
                        refs[lastRefName] = existing with { PeeledTarget = peeled };
                    }

                    continue;
                }

                var spaceIndex = line.IndexOf(' ');
                if (spaceIndex <= 0)
                {
                    lastRefName = null;
                    continue;
                }

                var hashText = line[..spaceIndex];
                var name = line[(spaceIndex + 1)..];
                if (GitObjectId.TryParse(hashText, objectFormat, out var id))
                {
                    refs.TryAdd(name, new RawRef(id, null));
                    lastRefName = name;
                }
                else
                {
                    lastRefName = null;
                }
            }
        }

        return refs;
    }

    private static async Task<GitObjectId?> ResolveHeadAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        IReadOnlyDictionary<string, RawRef> refs,
        CancellationToken cancellationToken)
    {
        var headPath = Path.Combine(gitDirectory, "HEAD");
        if (!File.Exists(headPath))
        {
            return null;
        }

        var text = (await File.ReadAllTextAsync(headPath, cancellationToken).ConfigureAwait(false)).Trim();
        const string refPrefix = "ref:";
        if (text.StartsWith(refPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var refName = text[refPrefix.Length..].Trim();
            return refs.TryGetValue(refName, out var rawRef) ? rawRef.Target : null;
        }

        return GitObjectId.TryParse(text, objectFormat, out var detachedId) ? detachedId : null;
    }

    private static async Task<GitObjectId?> ResolveCommitTargetAsync(
        GitObjectStore objectStore,
        GitObjectFormat objectFormat,
        GitObjectId id,
        CancellationToken cancellationToken)
    {
        var current = id;
        for (var depth = 0; depth < 8; depth++)
        {
            var data = await objectStore.ReadObjectAsync(current, cancellationToken).ConfigureAwait(false);
            if (data.Kind == GitObjectKind.Commit)
            {
                return current;
            }

            if (data.Kind != GitObjectKind.Tag)
            {
                return null;
            }

            var tag = ParseTag(current, objectFormat, data.Data);
            if (!tag.TargetType.Equals("commit", StringComparison.Ordinal) &&
                !tag.TargetType.Equals("tag", StringComparison.Ordinal))
            {
                return null;
            }

            current = tag.Target;
        }

        return null;
    }

    private static GitCommit ParseCommit(GitObjectId id, byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        var separator = text.IndexOf("\n\n", StringComparison.Ordinal);
        var headerText = separator >= 0 ? text[..separator] : text;
        var body = separator >= 0 ? text[(separator + 2)..] : string.Empty;
        var commit = new GitCommit { Hash = id, Body = body };

        foreach (var line in headerText.Split('\n'))
        {
            if (line.StartsWith("parent ", StringComparison.Ordinal))
            {
                commit.ParentHashes.Add(GitObjectId.Parse(line["parent ".Length..].Trim(), id.ObjectFormat));
            }
            else if (line.StartsWith("author ", StringComparison.Ordinal))
            {
                var author = ParseSignature(line["author ".Length..].Trim());
                commit.AuthorName = author.Name;
                commit.AuthorEmail = author.Email;
                commit.AuthorUnixSeconds = author.UnixSeconds;
            }
        }

        var trimmedBody = body.Trim('\n', '\r');
        var newline = trimmedBody.IndexOf('\n');
        commit.Subject = newline >= 0 ? trimmedBody[..newline].TrimEnd('\r') : trimmedBody;
        return commit;
    }

    private static GitTag ParseTag(GitObjectId id, GitObjectFormat objectFormat, byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        GitObjectId? target = null;
        var targetType = string.Empty;
        var name = string.Empty;

        foreach (var line in text.Split('\n'))
        {
            if (line.Length == 0)
            {
                break;
            }

            if (line.StartsWith("object ", StringComparison.Ordinal))
            {
                target = GitObjectId.Parse(line["object ".Length..].Trim(), objectFormat);
            }
            else if (line.StartsWith("type ", StringComparison.Ordinal))
            {
                targetType = line["type ".Length..].Trim();
            }
            else if (line.StartsWith("tag ", StringComparison.Ordinal))
            {
                name = line["tag ".Length..].Trim();
            }
        }

        if (target == null)
        {
            throw new InvalidDataException($"Tag object has no target: {id}");
        }

        return new GitTag(id, target.Value, name, targetType);
    }

    private static (string Name, string Email, long UnixSeconds) ParseSignature(string value)
    {
        var emailStart = value.LastIndexOf('<');
        var emailEnd = value.LastIndexOf('>');
        if (emailStart < 0 || emailEnd <= emailStart)
        {
            return (value, string.Empty, 0);
        }

        var name = value[..emailStart].Trim();
        var email = value[(emailStart + 1)..emailEnd].Trim();
        var rest = value[(emailEnd + 1)..].Trim();
        var firstSpace = rest.IndexOf(' ');
        var secondsText = firstSpace >= 0 ? rest[..firstSpace] : rest;
        return long.TryParse(secondsText, out var seconds)
            ? (name, email, seconds)
            : (name, email, 0);
    }

    private static GitRefKind GetRefKind(string fullName)
    {
        if (fullName.StartsWith("refs/heads/", StringComparison.Ordinal))
        {
            return GitRefKind.Head;
        }

        if (fullName.StartsWith("refs/remotes/", StringComparison.Ordinal))
        {
            return GitRefKind.Remote;
        }

        return fullName.StartsWith("refs/tags/", StringComparison.Ordinal)
            ? GitRefKind.Tag
            : GitRefKind.Other;
    }

    private static string GetDisplayRefName(string fullName, GitRefKind kind)
    {
        return kind switch
        {
            GitRefKind.Head => fullName["refs/heads/".Length..],
            GitRefKind.Remote => fullName["refs/remotes/".Length..],
            GitRefKind.Tag => fullName["refs/tags/".Length..],
            _ => fullName,
        };
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

    private readonly record struct RawRef(GitObjectId Target, GitObjectId? PeeledTarget);
}
