namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

internal static partial class GitRefReader
{
    public const int DefaultTagLimit = 500;

    private static async Task<Dictionary<string, GitRawRef>> LoadRefsCoreAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        int maxTags,
        CancellationToken cancellationToken)
    {
        var refs = new Dictionary<string, GitRawRef>(StringComparer.Ordinal);
        var tagCount = 0;
        var looseRefs = await GitLooseRefFileEnumerator.ReadSummaryRefsAsync(
            gitDirectory,
            objectFormat,
            maxTags,
            cancellationToken).ConfigureAwait(false);
        foreach (var file in looseRefs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var kind = GetRefKind(file.FullName);
            if (kind == GitRefKind.Tag) tagCount++;
            refs[file.FullName] = new GitRawRef(
                file.Target,
                null,
                RequiresPeeling: kind == GitRefKind.Tag);
        }

        var packedRefsPath = Path.Combine(gitDirectory, "packed-refs");
        if (File.Exists(packedRefsPath))
        {
            string? lastRefName = null;
            var fullyPeeled = false;
            await foreach (var rawLine in File
                               .ReadLinesAsync(packedRefsPath, cancellationToken)
                               .ConfigureAwait(false))
            {
                var line = rawLine.AsSpan().Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (line[0] == '#')
                {
                    fullyPeeled |= line.Contains("fully-peeled", StringComparison.Ordinal);
                    continue;
                }

                if (line[0] == '^')
                {
                    if (lastRefName != null && GitObjectId.TryParse(line[1..], objectFormat, out var peeled))
                    {
                        var existing = refs[lastRefName];
                        refs[lastRefName] = existing with
                        {
                            PeeledTarget = peeled,
                            RequiresPeeling = false,
                        };
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
                var name = line[(spaceIndex + 1)..].ToString();
                if (refs.ContainsKey(name) ||
                    !GitObjectId.TryParse(hashText, objectFormat, out var id) ||
                    ShouldSkipTag(name, maxTags, ref tagCount))
                {
                    lastRefName = null;
                    continue;
                }

                refs.Add(
                    name,
                    new GitRawRef(
                        id,
                        null,
                        RequiresPeeling: GetRefKind(name) == GitRefKind.Tag && !fullyPeeled));
                lastRefName = name;
            }
        }

        await LoadStashReflogRefsAsync(gitDirectory, objectFormat, refs, cancellationToken)
            .ConfigureAwait(false);
        return refs;
    }

    private static bool ShouldSkipTag(string fullName, int maxTags, ref int tagCount)
    {
        if (!fullName.StartsWith("refs/tags/", StringComparison.Ordinal))
        {
            return false;
        }

        return tagCount++ >= Math.Max(0, maxTags);
    }

    private static async Task LoadStashReflogRefsAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        Dictionary<string, GitRawRef> refs,
        CancellationToken cancellationToken)
    {
        var stashLogPath = Path.Combine(gitDirectory, "logs", "refs", "stash");
        if (!File.Exists(stashLogPath))
        {
            return;
        }

        var lines = await File.ReadAllLinesAsync(stashLogPath, cancellationToken)
            .ConfigureAwait(false);
        var stashIndex = 0;
        for (var index = lines.Length - 1; index >= 0; index--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = lines[index].AsSpan();
            var firstSpace = line.IndexOf(' ');
            if (firstSpace < 0)
            {
                continue;
            }

            var rest = line[(firstSpace + 1)..];
            var secondSpace = rest.IndexOf(' ');
            if (secondSpace < 0 ||
                !GitObjectId.TryParse(rest[..secondSpace], objectFormat, out var id))
            {
                continue;
            }

            var refName = stashIndex == 0 ? "refs/stash" : $"refs/stash@{{{stashIndex}}}";
            refs.TryAdd(refName, new GitRawRef(id, null, RequiresPeeling: false));
            stashIndex++;
        }
    }

    public static async Task<GitObjectId?> ResolveHeadAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        IReadOnlyDictionary<string, GitRawRef> refs,
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
            var refName = text.AsSpan(refPrefix.Length).Trim().ToString();
            return refs.TryGetValue(refName, out var rawRef) ? rawRef.Target : null;
        }

        return GitObjectId.TryParse(text, objectFormat, out var detachedId) ? detachedId : null;
    }

    public static async Task<string?> ResolveHeadBranchNameAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var headPath = Path.Combine(gitDirectory, "HEAD");
        if (!File.Exists(headPath))
        {
            return null;
        }

        var text = (await File.ReadAllTextAsync(headPath, cancellationToken).ConfigureAwait(false)).Trim();
        const string headPrefix = "ref: refs/heads/";
        return text.StartsWith(headPrefix, StringComparison.Ordinal)
            ? text[headPrefix.Length..]
            : null;
    }

    public static GitRefKind GetRefKind(string fullName)
    {
        if (fullName.StartsWith("refs/heads/", StringComparison.Ordinal))
        {
            return GitRefKind.Head;
        }

        if (fullName.StartsWith("refs/remotes/", StringComparison.Ordinal))
        {
            return GitRefKind.Remote;
        }

        if (fullName.StartsWith("refs/tags/", StringComparison.Ordinal))
        {
            return GitRefKind.Tag;
        }

        return fullName.Equals("refs/stash", StringComparison.Ordinal) ||
               fullName.StartsWith("refs/stash@{", StringComparison.Ordinal)
            ? GitRefKind.Stash
            : GitRefKind.Other;
    }

    public static string GetDisplayRefName(string fullName, GitRefKind kind)
    {
        return kind switch
        {
            GitRefKind.Head => fullName["refs/heads/".Length..],
            GitRefKind.Remote => fullName["refs/remotes/".Length..],
            GitRefKind.Tag => fullName["refs/tags/".Length..],
            GitRefKind.Stash => fullName.Equals("refs/stash", StringComparison.Ordinal)
                ? "stash"
                : fullName["refs/".Length..],
            _ => fullName,
        };
    }
}

internal readonly record struct GitRawRef(
    GitObjectId Target,
    GitObjectId? PeeledTarget,
    bool RequiresPeeling);
