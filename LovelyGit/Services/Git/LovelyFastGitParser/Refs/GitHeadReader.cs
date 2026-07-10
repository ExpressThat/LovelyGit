namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

internal static class GitHeadReader
{
    public static async Task<GitObjectId?> ResolveAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var headPath = Path.Combine(gitDirectory, "HEAD");
        if (!File.Exists(headPath))
        {
            return null;
        }

        var head = (await File.ReadAllTextAsync(headPath, cancellationToken).ConfigureAwait(false)).AsSpan().Trim();
        const string RefPrefix = "ref:";
        if (!head.StartsWith(RefPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return GitObjectId.TryParse(head, objectFormat, out var detachedId) ? detachedId : null;
        }

        var refName = head[RefPrefix.Length..].Trim().ToString();
        var looseRefPath = Path.Combine(gitDirectory, refName.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(looseRefPath))
        {
            var value = (await File.ReadAllTextAsync(looseRefPath, cancellationToken).ConfigureAwait(false))
                .AsSpan()
                .Trim();
            return GitObjectId.TryParse(value, objectFormat, out var looseId) ? looseId : null;
        }

        return await ResolvePackedRefAsync(
            gitDirectory,
            refName,
            objectFormat,
            cancellationToken).ConfigureAwait(false);
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
