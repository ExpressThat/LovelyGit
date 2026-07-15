namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

internal static class GitHeadReader
{
    public static async Task<GitObjectId?> ResolveAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken) =>
        await ResolveAsync(gitDirectory, gitDirectory, objectFormat, cancellationToken)
            .ConfigureAwait(false);

    public static async Task<GitObjectId?> ResolveAsync(
        string headGitDirectory,
        string commonGitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken) =>
        (await ReadAsync(
                headGitDirectory,
                commonGitDirectory,
                objectFormat,
                cancellationToken)
            .ConfigureAwait(false)).Target;

    public static async Task<GitHeadState> ReadAsync(
        string headGitDirectory,
        string commonGitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var headPath = Path.Combine(headGitDirectory, "HEAD");
        if (!File.Exists(headPath))
        {
            return default;
        }

        var head = (await File.ReadAllTextAsync(headPath, cancellationToken).ConfigureAwait(false)).AsSpan().Trim();
        const string RefPrefix = "ref:";
        if (!head.StartsWith(RefPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new GitHeadState(
                null,
                GitObjectId.TryParse(head, objectFormat, out var detachedId) ? detachedId : null);
        }

        var refName = head[RefPrefix.Length..].Trim().ToString();
        const string HeadPrefix = "refs/heads/";
        var branchName = refName.StartsWith(HeadPrefix, StringComparison.Ordinal)
            ? refName[HeadPrefix.Length..]
            : null;
        var looseRefPath = Path.Combine(commonGitDirectory, refName.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(looseRefPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new GitHeadState(
                branchName,
                GitLooseRefReader.TryReadObjectId(looseRefPath, objectFormat, out var looseId)
                    ? looseId
                    : null);
        }

        return new GitHeadState(
            branchName,
            await ResolvePackedRefAsync(
                    commonGitDirectory,
                    refName,
                    objectFormat,
                    cancellationToken)
                .ConfigureAwait(false));
    }

    private static async Task<GitObjectId?> ResolvePackedRefAsync(
        string gitDirectory,
        string refName,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(gitDirectory, "packed-refs");
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            var value = line.AsSpan();
            if (value.Length == 0 || value[0] is '#' or '^')
            {
                continue;
            }

            var separator = value.IndexOf(' ');
            if (separator <= 0 || !value[(separator + 1)..].SequenceEqual(refName))
            {
                continue;
            }

            return GitObjectId.TryParse(value[..separator], objectFormat, out var packedId)
                ? packedId
                : null;
        }

        return null;
    }
}

internal readonly record struct GitHeadState(string? BranchName, GitObjectId? Target);
