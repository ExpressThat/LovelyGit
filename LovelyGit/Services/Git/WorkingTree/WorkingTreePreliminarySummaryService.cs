using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreePreliminarySummaryService
{
    public async Task<WorkingTreeChangeSummaryResponse> GetSummaryAsync(
        string workTreeDirectory,
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var trackedEntryCount = await GitIndexHeaderReader
            .ReadEntryCountAsync(gitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var shouldPreloadChanges = trackedEntryCount is not uint trackedCount ||
            !WorkingTreeStatusScanPolicy.ShouldSkipNativeScanBeforeRootTracking(trackedCount);
        var candidates = Directory.EnumerateFileSystemEntries(workTreeDirectory)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => !name.Equals(".git", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length == 0)
        {
            return Incomplete(0, shouldPreloadChanges);
        }

        var missingCount = CountRootEntriesMissingFromIndexCached(
            gitDirectory,
            candidates,
            cancellationToken);
        return Incomplete(missingCount, shouldPreloadChanges);
    }

    private static int CountRootEntriesMissingFromIndex(
        string gitDirectory,
        IReadOnlyList<string> candidates,
        CancellationToken cancellationToken) =>
        WorkingTreePreliminaryIndexReader.CountMissingRootEntries(
            Path.Combine(gitDirectory, "index"),
            candidates,
            cancellationToken);

    private static WorkingTreeChangeSummaryResponse Incomplete(
        int totalCount,
        bool shouldPreloadChanges) => new()
    {
        IsComplete = false,
        ShouldPreloadChanges = shouldPreloadChanges,
        TotalCount = totalCount,
    };
}
