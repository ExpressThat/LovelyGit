using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.BranchComparison;

internal static partial class NativeBranchComparisonReader
{
    private static async Task<BranchComparisonResponse> BuildResponseAsync(
        LovelyGitRepository repository,
        GitObjectId current,
        GitObjectId target,
        string currentLabel,
        string targetLabel,
        CancellationToken cancellationToken)
    {
        var history = await PaintHistoryAsync(
            repository, current, target, MaximumHistoryNodes, cancellationToken)
            .ConfigureAwait(false);
        var tips = await Task.WhenAll(
            repository.GetCommitAsync(current, cancellationToken),
            repository.GetCommitAsync(target, cancellationToken)).ConfigureAwait(false);
        var comparison = await repository.GetChangedTreeFilesAsync(
            tips[0].TreeHash, tips[1].TreeHash, cancellationToken).ConfigureAwait(false);
        var files = BuildFiles(comparison.ParentFiles, comparison.CurrentFiles);
        return new BranchComparisonResponse
        {
            CurrentBranchName = currentLabel,
            TargetBranchName = targetLabel,
            CurrentHash = current.ToString(),
            TargetHash = target.ToString(),
            MergeBaseHash = history.MergeBaseHash?.ToString(),
            AheadCount = history.Ahead.Count,
            BehindCount = history.Behind.Count,
            ChangedFileCount = files.Count,
            IsHistoryPartial = history.IsPartial,
            IsFileListTruncated = files.Count > MaximumDisplayedFiles,
            AheadCommits = await BuildCommitsAsync(
                repository, history.Ahead, cancellationToken).ConfigureAwait(false),
            BehindCommits = await BuildCommitsAsync(
                repository, history.Behind, cancellationToken).ConfigureAwait(false),
            Files = files.Take(MaximumDisplayedFiles).ToList(),
        };
    }
}
