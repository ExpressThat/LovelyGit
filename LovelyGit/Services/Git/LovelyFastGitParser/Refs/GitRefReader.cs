namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

internal static class GitRefReader
{
    public static async Task<Dictionary<string, GitRawRef>> LoadRefsAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var refs = new Dictionary<string, GitRawRef>(StringComparer.Ordinal);
        var refsDirectory = Path.Combine(gitDirectory, "refs");
        if (Directory.Exists(refsDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(refsDirectory, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var text = (await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false)).Trim();
                if (GitObjectId.TryParse(text, objectFormat, out var id))
                {
                    refs[Path.GetRelativePath(gitDirectory, file).Replace('\\', '/')] = new GitRawRef(id, null);
                }
            }
        }

        var packedRefsPath = Path.Combine(gitDirectory, "packed-refs");
        if (File.Exists(packedRefsPath))
        {
            string? lastRefName = null;
            foreach (var rawLine in await File.ReadAllLinesAsync(packedRefsPath, cancellationToken).ConfigureAwait(false))
            {
                var line = rawLine.AsSpan().Trim();
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
                var name = line[(spaceIndex + 1)..].ToString();
                if (GitObjectId.TryParse(hashText, objectFormat, out var id))
                {
                    refs.TryAdd(name, new GitRawRef(id, null));
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

        return fullName.StartsWith("refs/tags/", StringComparison.Ordinal)
            ? GitRefKind.Tag
            : GitRefKind.Other;
    }

    public static string GetDisplayRefName(string fullName, GitRefKind kind)
    {
        return kind switch
        {
            GitRefKind.Head => fullName["refs/heads/".Length..],
            GitRefKind.Remote => fullName["refs/remotes/".Length..],
            GitRefKind.Tag => fullName["refs/tags/".Length..],
            _ => fullName,
        };
    }
}

internal readonly record struct GitRawRef(GitObjectId Target, GitObjectId? PeeledTarget);
