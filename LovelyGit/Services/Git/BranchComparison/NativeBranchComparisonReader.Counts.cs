using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.BranchComparison;

internal static partial class NativeBranchComparisonReader
{
    public static async Task<HistoryDivergenceCounts> CountHistoryAsync(
        LovelyGitRepository repository,
        GitObjectId current,
        GitObjectId target,
        CancellationToken cancellationToken)
    {
        var history = await PaintHistoryAsync(
            repository,
            current,
            target,
            MaximumHistoryNodes,
            cancellationToken).ConfigureAwait(false);
        return new HistoryDivergenceCounts(
            history.Ahead.Count,
            history.Behind.Count,
            history.IsPartial);
    }
}

internal readonly record struct HistoryDivergenceCounts(
    int AheadCount,
    int BehindCount,
    bool IsPartial);
