namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

internal static class GitRefSummaryReader
{
    public static async Task<GitRefSummary> ReadAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        int maxTags,
        CancellationToken cancellationToken)
    {
        var refs = new List<GitRef>();
        var seenFullNames = new HashSet<string>(StringComparer.Ordinal);
        var remotePrefixes = new HashSet<string>(StringComparer.Ordinal);
        var tagCount = 0;
        ReadLooseRefs(
            gitDirectory,
            objectFormat,
            refs,
            seenFullNames,
            remotePrefixes,
            maxTags,
            tagCount,
            cancellationToken);
        tagCount = refs.Count(reference => reference.Kind == GitRefKind.Tag);
        await ReadPackedRefsAsync(gitDirectory, objectFormat, refs, seenFullNames, remotePrefixes, maxTags, tagCount, cancellationToken)
            .ConfigureAwait(false);
        await ReadStashRefAsync(gitDirectory, objectFormat, refs, cancellationToken).ConfigureAwait(false);
        var currentBranchName = await GitRefReader.ResolveHeadBranchNameAsync(gitDirectory, cancellationToken)
            .ConfigureAwait(false);

        return new GitRefSummary(
            currentBranchName,
            remotePrefixes.Order(StringComparer.Ordinal).ToArray(),
            refs.OrderBy(reference => reference.Kind).ThenBy(reference => reference.Name, StringComparer.Ordinal).ToArray());
    }

    private static void ReadLooseRefs(
        string gitDirectory,
        GitObjectFormat objectFormat,
        List<GitRef> refs,
        HashSet<string> seenFullNames,
        HashSet<string> remotePrefixes,
        int maxTags,
        int tagCount,
        CancellationToken cancellationToken)
    {
        var refsDirectory = Path.Combine(gitDirectory, "refs");
        if (!Directory.Exists(refsDirectory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(refsDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullName = Path.GetRelativePath(gitDirectory, file).Replace('\\', '/');
            var text = File.ReadAllText(file).AsSpan().Trim();
            if (GitObjectId.TryParse(text, objectFormat, out var id))
            {
                AddRef(refs, seenFullNames, remotePrefixes, fullName, id, maxTags, ref tagCount);
            }
        }
    }

    private static async Task ReadPackedRefsAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        List<GitRef> refs,
        HashSet<string> seenFullNames,
        HashSet<string> remotePrefixes,
        int maxTags,
        int tagCount,
        CancellationToken cancellationToken)
    {
        var packedRefsPath = Path.Combine(gitDirectory, "packed-refs");
        if (!File.Exists(packedRefsPath))
        {
            return;
        }

        await foreach (var rawLine in File.ReadLinesAsync(packedRefsPath, cancellationToken).ConfigureAwait(false))
        {
            var line = rawLine.AsSpan().Trim();
            if (line.Length == 0 || line[0] is '#' or '^')
            {
                continue;
            }

            var spaceIndex = line.IndexOf(' ');
            if (spaceIndex <= 0)
            {
                continue;
            }

            var fullName = line[(spaceIndex + 1)..].ToString();
            if (seenFullNames.Contains(fullName) ||
                !GitObjectId.TryParse(line[..spaceIndex], objectFormat, out var id))
            {
                continue;
            }

            AddRef(refs, seenFullNames, remotePrefixes, fullName, id, maxTags, ref tagCount);
        }
    }

    private static async Task ReadStashRefAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        List<GitRef> refs,
        CancellationToken cancellationToken)
    {
        var stashPath = Path.Combine(gitDirectory, "refs", "stash");
        if (!File.Exists(stashPath))
        {
            return;
        }

        var text = (await File.ReadAllTextAsync(stashPath, cancellationToken).ConfigureAwait(false)).Trim();
        if (GitObjectId.TryParse(text, objectFormat, out var id))
        {
            refs.Add(new GitRef("stash", id, GitRefKind.Stash));
        }
    }

    private static void AddRef(
        List<GitRef> refs,
        HashSet<string> seenFullNames,
        HashSet<string> remotePrefixes,
        string fullName,
        GitObjectId target,
        int maxTags,
        ref int tagCount)
    {
        var kind = GitRefReader.GetRefKind(fullName);
        if (kind == GitRefKind.Tag && tagCount++ >= maxTags)
        {
            return;
        }

        if (kind is not (GitRefKind.Head or GitRefKind.Remote or GitRefKind.Tag))
        {
            return;
        }

        seenFullNames.Add(fullName);
        var name = GitRefReader.GetDisplayRefName(fullName, kind);
        refs.Add(new GitRef(name, target, kind));
        if (kind == GitRefKind.Remote)
        {
            AddRemotePrefix(remotePrefixes, name);
        }
    }

    private static void AddRemotePrefix(HashSet<string> remotePrefixes, string displayName)
    {
        var slashIndex = displayName.IndexOf('/', StringComparison.Ordinal);
        if (slashIndex > 0)
        {
            remotePrefixes.Add(displayName[..slashIndex]);
        }
    }
}

internal sealed record GitRefSummary(
    string? CurrentBranchName,
    IReadOnlyList<string> RemotePrefixes,
    IReadOnlyList<GitRef> Refs);
