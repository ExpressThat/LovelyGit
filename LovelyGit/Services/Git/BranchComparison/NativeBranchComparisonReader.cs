using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.BranchComparison;

internal static partial class NativeBranchComparisonReader
{
    public const int MaximumHistoryNodes = 200_000;
    public const int MaximumDisplayedCommits = 100;
    public const int MaximumDisplayedFiles = 500;

    public static async Task<BranchComparisonResponse> ReadAsync(
        string repositoryPath,
        string targetBranchName,
        CancellationToken cancellationToken)
    {
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var currentName = repository.CurrentBranchName ??
            throw new InvalidOperationException("Check out a branch before comparing branches.");
        var currentHash = repository.HeadTarget ??
            throw new InvalidOperationException("The current branch does not have a commit.");
        var targetName = NormalizeBranchName(targetBranchName);
        var target = repository.GetBranches().FirstOrDefault(reference =>
            reference.Kind == GitRefKind.Head &&
            string.Equals(reference.Name, targetName, StringComparison.Ordinal));
        if (target == null)
        {
            throw new ArgumentException("The target local branch was not found.", nameof(targetBranchName));
        }

        var history = await PaintHistoryAsync(
            repository, currentHash, target.Target, MaximumHistoryNodes, cancellationToken)
            .ConfigureAwait(false);
        var tips = await Task.WhenAll(
            repository.GetCommitAsync(currentHash, cancellationToken),
            repository.GetCommitAsync(target.Target, cancellationToken)).ConfigureAwait(false);
        var comparison = await repository.GetChangedTreeFilesAsync(
            tips[0].TreeHash, tips[1].TreeHash, cancellationToken).ConfigureAwait(false);
        var files = BuildFiles(comparison.ParentFiles, comparison.CurrentFiles);

        return new BranchComparisonResponse
        {
            CurrentBranchName = currentName,
            TargetBranchName = targetName,
            CurrentHash = currentHash.ToString(),
            TargetHash = target.Target.ToString(),
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

    private static string NormalizeBranchName(string branchName)
    {
        var value = branchName.Trim();
        if (value.Length == 0 || value.Length > 255 || value.StartsWith('-') || value.Any(char.IsControl))
        {
            throw new ArgumentException("The target branch name is invalid.", nameof(branchName));
        }

        return value;
    }
}
