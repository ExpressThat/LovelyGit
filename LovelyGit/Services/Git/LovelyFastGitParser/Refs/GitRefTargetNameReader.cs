namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

internal static class GitRefTargetNameReader
{
    public static async Task<string?> FindBranchNameAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        GitObjectId target,
        CancellationToken cancellationToken)
    {
        var local = FindLoose(
            Path.Combine(gitDirectory, "refs", "heads"),
            objectFormat,
            target,
            cancellationToken);
        if (local != null)
        {
            return local;
        }

        var remote = FindLoose(
            Path.Combine(gitDirectory, "refs", "remotes"),
            objectFormat,
            target,
            cancellationToken);
        if (remote != null)
        {
            return remote;
        }

        return await FindPackedAsync(
            gitDirectory, objectFormat, target, cancellationToken).ConfigureAwait(false);
    }

    private static string? FindLoose(
        string directory,
        GitObjectFormat objectFormat,
        GitObjectId target,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(directory))
        {
            return null;
        }

        string? match = null;
        foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (GitLooseRefReader.TryReadObjectId(path, objectFormat, out var candidate) &&
                candidate == target)
            {
                var candidateName = Path.GetRelativePath(directory, path).Replace('\\', '/');
                if (match == null || string.CompareOrdinal(candidateName, match) < 0)
                {
                    match = candidateName;
                }
            }
        }

        return match;
    }

    private static async Task<string?> FindPackedAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        GitObjectId target,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(gitDirectory, "packed-refs");
        if (!File.Exists(path))
        {
            return null;
        }

        string? localMatch = null;
        string? remoteMatch = null;
        await foreach (var rawLine in File.ReadLinesAsync(path, cancellationToken).ConfigureAwait(false))
        {
            var line = rawLine.AsSpan();
            var separator = line.IndexOf(' ');
            if (separator <= 0 ||
                !GitObjectId.TryParse(line[..separator], objectFormat, out var candidate) ||
                candidate != target)
            {
                continue;
            }

            var fullName = line[(separator + 1)..];
            var kind = fullName.StartsWith("refs/heads/", StringComparison.Ordinal)
                ? GitRefKind.Head
                : fullName.StartsWith("refs/remotes/", StringComparison.Ordinal)
                    ? GitRefKind.Remote
                    : GitRefKind.Other;
            if (kind == GitRefKind.Other || HasLooseOverride(gitDirectory, fullName))
            {
                continue;
            }

            var displayName = GitRefReader.GetDisplayRefName(fullName.ToString(), kind);
            if (kind == GitRefKind.Head)
            {
                if (localMatch == null || string.CompareOrdinal(displayName, localMatch) < 0)
                {
                    localMatch = displayName;
                }
            }
            else if (remoteMatch == null || string.CompareOrdinal(displayName, remoteMatch) < 0)
            {
                remoteMatch = displayName;
            }
        }

        return localMatch ?? remoteMatch;
    }

    private static bool HasLooseOverride(string gitDirectory, ReadOnlySpan<char> fullName) =>
        File.Exists(Path.Combine(
            gitDirectory,
            fullName.ToString().Replace('/', Path.DirectorySeparatorChar)));
}
