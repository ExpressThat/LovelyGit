namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

internal static class GitLooseRefFileEnumerator
{
    public static IEnumerable<GitLooseRefFile> Enumerate(
        string gitDirectory,
        GitObjectFormat objectFormat,
        int maxTags,
        CancellationToken cancellationToken = default)
    {
        var refsDirectory = Path.Combine(gitDirectory, "refs");
        foreach (var file in EnumerateNamespace(
                     refsDirectory, "heads", objectFormat, int.MaxValue, cancellationToken))
            yield return file;
        foreach (var file in EnumerateNamespace(
                     refsDirectory, "remotes", objectFormat, int.MaxValue, cancellationToken))
            yield return file;
        foreach (var file in EnumerateNamespace(
                     refsDirectory,
                     "tags",
                     objectFormat,
                     Math.Max(0, maxTags),
                     cancellationToken))
            yield return file;

        cancellationToken.ThrowIfCancellationRequested();
        var stashPath = Path.Combine(refsDirectory, "stash");
        if (File.Exists(stashPath) &&
            GitLooseRefReader.TryReadObjectId(stashPath, objectFormat, out var stashTarget))
            yield return new GitLooseRefFile(stashPath, "refs/stash", stashTarget);
    }

    private static IEnumerable<GitLooseRefFile> EnumerateNamespace(
        string refsDirectory,
        string name,
        GitObjectFormat objectFormat,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit == 0) yield break;
        var directory = Path.Combine(refsDirectory, name);
        if (!Directory.Exists(directory)) yield break;

        var count = 0;
        foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!GitLooseRefReader.TryReadObjectId(path, objectFormat, out var target)) continue;
            if (count++ >= limit) yield break;
            yield return new GitLooseRefFile(
                path,
                Path.GetRelativePath(refsDirectory, path).Replace('\\', '/').Insert(0, "refs/"),
                target);
        }
    }
}

internal readonly record struct GitLooseRefFile(
    string Path,
    string FullName,
    GitObjectId Target);
