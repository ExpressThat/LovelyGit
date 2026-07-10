using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.BranchComparison;

internal static partial class NativeBranchComparisonReader
{
    private static async Task<List<BranchComparisonCommit>> BuildCommitsAsync(
        LovelyGitRepository repository,
        IReadOnlyList<PaintedCommit> commits,
        CancellationToken cancellationToken)
    {
        var count = Math.Min(commits.Count, MaximumDisplayedCommits);
        var results = new List<BranchComparisonCommit>(count);
        for (var index = 0; index < count; index++)
        {
            var commit = await repository.GetCommitAsync(commits[index].Hash, cancellationToken)
                .ConfigureAwait(false);
            results.Add(new BranchComparisonCommit
            {
                Hash = commit.Hash.ToString(),
                Subject = commit.Subject,
                AuthorName = commit.AuthorName,
                AuthorUnixSeconds = commit.AuthorUnixSeconds,
            });
        }

        return results;
    }

    private static List<BranchComparisonFile> BuildFiles(
        IReadOnlyDictionary<string, GitTreeFile> current,
        IReadOnlyDictionary<string, GitTreeFile> target)
    {
        var paths = current.Keys.Concat(target.Keys).Distinct(StringComparer.Ordinal);
        var files = new List<BranchComparisonFile>();
        foreach (var path in paths)
        {
            current.TryGetValue(path, out var oldFile);
            target.TryGetValue(path, out var newFile);
            var status = oldFile == null
                ? "Added"
                : newFile == null
                    ? "Deleted"
                    : oldFile.Mode == newFile.Mode ? "Modified" : "TypeChanged";
            files.Add(new BranchComparisonFile { Path = path, Status = status });
        }

        files.Sort(static (left, right) => string.CompareOrdinal(left.Path, right.Path));
        return files;
    }
}
