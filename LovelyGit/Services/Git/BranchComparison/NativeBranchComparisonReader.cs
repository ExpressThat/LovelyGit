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
            reference.Kind is GitRefKind.Head or GitRefKind.Remote &&
            string.Equals(reference.Name, targetName, StringComparison.Ordinal));
        if (target == null)
        {
            throw new ArgumentException("The target branch was not found.", nameof(targetBranchName));
        }

        return await BuildResponseAsync(
            repository,
            currentHash,
            target.Target,
            currentName,
            targetName,
            cancellationToken).ConfigureAwait(false);
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
