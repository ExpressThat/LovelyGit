namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

internal static class GitCommitRefReader
{
    public static async Task<IReadOnlyList<GitCommitRef>> ReadAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        GitObjectId commitId,
        Func<GitObjectId, CancellationToken, Task<GitObjectId?>> peelTagAsync,
        CancellationToken cancellationToken)
    {
        var refs = new List<GitCommitRef>();
        var tagCount = await ReadLooseRefsAsync(
                gitDirectory,
                objectFormat,
                commitId,
                refs,
                peelTagAsync,
                cancellationToken)
            .ConfigureAwait(false);
        await ReadPackedRefsAsync(
                gitDirectory,
                objectFormat,
                commitId,
                refs,
                peelTagAsync,
                tagCount,
                cancellationToken)
            .ConfigureAwait(false);
        return refs;
    }

    private static async Task<int> ReadLooseRefsAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        GitObjectId commitId,
        List<GitCommitRef> refs,
        Func<GitObjectId, CancellationToken, Task<GitObjectId?>> peelTagAsync,
        CancellationToken cancellationToken)
    {
        var refsDirectory = Path.Combine(gitDirectory, "refs");
        if (!Directory.Exists(refsDirectory)) return 0;

        var tagCount = 0;
        foreach (var path in Directory.EnumerateFiles(refsDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullName = Path.GetRelativePath(gitDirectory, path).Replace('\\', '/');
            var kind = GitRefReader.GetRefKind(fullName);
            if (kind == GitRefKind.Tag && tagCount++ >= GitRefReader.DefaultTagLimit) continue;
            if (kind is not (GitRefKind.Head or GitRefKind.Remote or GitRefKind.Tag) ||
                !GitLooseRefReader.TryReadObjectId(path, objectFormat, out var target)) continue;

            var matches = target == commitId;
            if (!matches && kind == GitRefKind.Tag)
            {
                matches = await peelTagAsync(target, cancellationToken).ConfigureAwait(false) == commitId;
            }

            if (matches) Add(refs, fullName, kind);
        }

        return tagCount;
    }

    private static async Task ReadPackedRefsAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        GitObjectId commitId,
        List<GitCommitRef> refs,
        Func<GitObjectId, CancellationToken, Task<GitObjectId?>> peelTagAsync,
        int tagCount,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(gitDirectory, "packed-refs");
        if (!File.Exists(path)) return;

        var fullyPeeled = false;
        PendingTag? pending = null;
        await foreach (var rawLine in File.ReadLinesAsync(path, cancellationToken).ConfigureAwait(false))
        {
            var parsed = ParsePackedLine(rawLine, objectFormat);
            if (parsed.Kind == PackedLineKind.Empty) continue;
            if (parsed.Kind == PackedLineKind.Comment)
            {
                fullyPeeled |= parsed.FullyPeeled;
                continue;
            }

            if (parsed.Kind == PackedLineKind.Peeled)
            {
                if (pending is { } tag &&
                    parsed.Target == commitId) Add(refs, tag.FullName, GitRefKind.Tag);
                pending = null;
                continue;
            }

            if (pending is { } previous && !fullyPeeled &&
                await peelTagAsync(previous.Target, cancellationToken).ConfigureAwait(false) == commitId)
            {
                Add(refs, previous.FullName, GitRefKind.Tag);
            }

            pending = null;
            var target = parsed.Target;
            var fullName = parsed.FullName!;
            var kind = GitRefReader.GetRefKind(fullName);
            if (HasLooseOverride(gitDirectory, fullName)) continue;
            if (kind == GitRefKind.Tag && tagCount++ >= GitRefReader.DefaultTagLimit) continue;
            if (kind is GitRefKind.Head or GitRefKind.Remote)
            {
                if (target == commitId) Add(refs, fullName, kind);
            }
            else if (kind == GitRefKind.Tag)
            {
                if (target == commitId) Add(refs, fullName, kind);
                else pending = new PendingTag(fullName, target);
            }
        }

        if (pending is { } last && !fullyPeeled &&
            await peelTagAsync(last.Target, cancellationToken).ConfigureAwait(false) == commitId)
        {
            Add(refs, last.FullName, GitRefKind.Tag);
        }
    }

    private static bool HasLooseOverride(string gitDirectory, string fullName) =>
        File.Exists(Path.Combine(gitDirectory, fullName.Replace('/', Path.DirectorySeparatorChar)));

    private static ParsedPackedLine ParsePackedLine(string rawLine, GitObjectFormat objectFormat)
    {
        var line = rawLine.AsSpan().Trim();
        if (line.Length == 0) return default;
        if (line[0] == '#')
        {
            return new ParsedPackedLine(
                PackedLineKind.Comment,
                default,
                null,
                line.Contains("fully-peeled", StringComparison.Ordinal));
        }

        if (line[0] == '^')
        {
            return GitObjectId.TryParse(line[1..], objectFormat, out var peeled)
                ? new ParsedPackedLine(PackedLineKind.Peeled, peeled, null, false)
                : default;
        }

        var separator = line.IndexOf(' ');
        return separator > 0 && GitObjectId.TryParse(line[..separator], objectFormat, out var target)
            ? new ParsedPackedLine(
                PackedLineKind.Ref,
                target,
                line[(separator + 1)..].ToString(),
                false)
            : default;
    }

    private static void Add(List<GitCommitRef> refs, string fullName, GitRefKind kind) =>
        refs.Add(new GitCommitRef(GitRefReader.GetDisplayRefName(fullName, kind), kind));

    private enum PackedLineKind { Empty, Comment, Peeled, Ref }
    private readonly record struct ParsedPackedLine(
        PackedLineKind Kind,
        GitObjectId Target,
        string? FullName,
        bool FullyPeeled);
    private readonly record struct PendingTag(string FullName, GitObjectId Target);
}
