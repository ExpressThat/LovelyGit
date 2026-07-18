namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

internal static class GitLooseRefFileEnumerator
{
    private const int MaxConcurrentReads = 32;

    public static async Task<IReadOnlyList<GitLooseRefFile>> ReadSummaryRefsAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        int maxTags,
        CancellationToken cancellationToken)
    {
        var refsDirectory = Path.Combine(gitDirectory, "refs");
        var paths = EnumeratePaths(refsDirectory, "heads")
            .Concat(EnumeratePaths(refsDirectory, "remotes"))
            .ToArray();
        var parsed = new GitLooseRefFile?[paths.Length];
        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = MaxConcurrentReads,
        };
        await Task.Run(
                () => Parallel.For(0, paths.Length, options, index =>
                {
                    var path = paths[index];
                    if (GitLooseRefReader.TryReadObjectId(path, objectFormat, out var target))
                    {
                        parsed[index] = CreateRef(refsDirectory, path, target);
                    }
                }),
                cancellationToken)
            .ConfigureAwait(false);

        var refs = new List<GitLooseRefFile>(paths.Length + Math.Max(0, maxTags));
        foreach (var reference in parsed)
        {
            if (reference is { } value) refs.Add(value);
        }

        refs.AddRange(EnumerateNamespace(
            refsDirectory,
            "tags",
            objectFormat,
            Math.Max(0, maxTags),
            cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
        var stashPath = Path.Combine(refsDirectory, "stash");
        if (File.Exists(stashPath) &&
            GitLooseRefReader.TryReadObjectId(stashPath, objectFormat, out var stashTarget))
        {
            refs.Add(new GitLooseRefFile(stashPath, "refs/stash", stashTarget));
        }

        return refs;
    }

    public static IEnumerable<string> EnumerateFingerprintPaths(
        string gitDirectory,
        GitObjectFormat objectFormat,
        int maxTags)
    {
        var refsDirectory = Path.Combine(gitDirectory, "refs");
        foreach (var path in EnumeratePaths(refsDirectory, "heads")) yield return path;
        foreach (var path in EnumeratePaths(refsDirectory, "remotes")) yield return path;
        foreach (var reference in EnumerateNamespace(
                     refsDirectory,
                     "tags",
                     objectFormat,
                     Math.Max(0, maxTags),
                     CancellationToken.None))
        {
            yield return reference.Path;
        }

        var stashPath = Path.Combine(refsDirectory, "stash");
        if (File.Exists(stashPath)) yield return stashPath;
    }

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
            yield return CreateRef(refsDirectory, path, target);
        }
    }

    private static IEnumerable<string> EnumeratePaths(string refsDirectory, string name)
    {
        var directory = Path.Combine(refsDirectory, name);
        return Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            : [];
    }

    private static GitLooseRefFile CreateRef(
        string refsDirectory,
        string path,
        GitObjectId target) =>
        new(
            path,
            Path.GetRelativePath(refsDirectory, path).Replace('\\', '/').Insert(0, "refs/"),
            target);
}

internal readonly record struct GitLooseRefFile(
    string Path,
    string FullName,
    GitObjectId Target);
