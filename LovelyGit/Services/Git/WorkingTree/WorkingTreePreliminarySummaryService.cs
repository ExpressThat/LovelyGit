using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreePreliminarySummaryService
{
    public Task<WorkingTreeChangeSummaryResponse> GetSummaryAsync(
        string workTreeDirectory,
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var candidates = Directory.EnumerateFileSystemEntries(workTreeDirectory)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => !name.Equals(".git", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length == 0)
        {
            return Task.FromResult(Incomplete(0));
        }

        var count = CountRootEntriesMissingFromIndexCached(gitDirectory, candidates, cancellationToken);
        return Task.FromResult(Incomplete(count));
    }

    private static int CountRootEntriesMissingFromIndex(
        string gitDirectory,
        IReadOnlyList<string> candidates,
        CancellationToken cancellationToken) =>
        WorkingTreePreliminaryIndexReader.CountMissingRootEntries(
            Path.Combine(gitDirectory, "index"),
            candidates,
            cancellationToken);

    private static WorkingTreeChangeSummaryResponse Incomplete(int totalCount) => new()
    {
        IsComplete = false,
        TotalCount = totalCount,
    };
}
