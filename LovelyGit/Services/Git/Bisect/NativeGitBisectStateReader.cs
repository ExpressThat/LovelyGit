using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.Bisect;

internal sealed class NativeGitBisectStateReader
{
    private const string BisectRefPrefix = "refs/bisect/";

    public async Task<GitBisectState> ReadAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var startPath = Path.Combine(paths.WorktreeGitDirectory, "BISECT_START");
        if (!File.Exists(startPath)) return new GitBisectState();

        using var repository = await LovelyGitRepository
            .OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var refs = await GitRefReader
            .LoadRefsAsync(paths.GitDirectory, repository.ObjectFormat, 0, cancellationToken)
            .ConfigureAwait(false);
        var currentHash = repository.HeadTarget;
        var current = currentHash.HasValue
            ? await repository.GetCommitAsync(currentHash.Value, cancellationToken)
                .ConfigureAwait(false)
            : null;
        var goodCommits = refs
            .Where(pair => pair.Key.StartsWith(BisectRefPrefix + "good-", StringComparison.Ordinal))
            .Select(pair => pair.Value.Target.ToString())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        var badCommit = refs.TryGetValue(BisectRefPrefix + "bad", out var badRef)
            ? badRef.Target.ToString()
            : null;
        return new GitBisectState
        {
            IsActive = true,
            StartingReference = await ReadTrimmedAsync(startPath, cancellationToken).ConfigureAwait(false),
            CurrentCommit = currentHash?.ToString(),
            CurrentSubject = current?.Subject,
            BadCommit = badCommit,
            GoodCommits = goodCommits,
            FirstBadCommit = await ReadFirstBadCommitAsync(
                    Path.Combine(paths.WorktreeGitDirectory, "BISECT_LOG"),
                    cancellationToken)
                .ConfigureAwait(false),
        };
    }

    private static async Task<string?> ReadTrimmedAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var value = (await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false)).Trim();
        return value.Length == 0 ? null : value;
    }

    private static async Task<string?> ReadFirstBadCommitAsync(
        string logPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(logPath)) return null;
        const string prefix = "# first bad commit: [";
        string? result = null;
        using var reader = new StreamReader(logPath);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (!line.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var end = line.IndexOf(']', prefix.Length);
            if (end > prefix.Length) result = line[prefix.Length..end];
        }

        return result;
    }
}
