using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.BranchComparison;

internal static partial class NativeBranchComparisonReader
{
    public const int MaximumHistoryNodes = 200_000;
    public const int MaximumDisplayedCommits = 100;
    public const int MaximumDisplayedFiles = 20_000;

    public static async Task<BranchComparisonResponse> ReadAsync(
        string repositoryPath,
        string targetBranchName,
        CancellationToken cancellationToken)
    {
        var targetName = NormalizeBranchName(targetBranchName);
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var objectFormat = await GitRepositoryDiscovery
            .ReadObjectFormatAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var head = await GitHeadReader.ReadAsync(
                paths.WorktreeGitDirectory,
                paths.GitDirectory,
                objectFormat,
                cancellationToken)
            .ConfigureAwait(false);
        var currentName = head.BranchName ??
            throw new InvalidOperationException("Check out a branch before comparing branches.");
        var currentHash = head.Target ??
            throw new InvalidOperationException("The current branch does not have a commit.");
        var target = await ResolveTargetAsync(
                paths.GitDirectory,
                targetName,
                objectFormat,
                cancellationToken)
            .ConfigureAwait(false);
        if (target == null)
        {
            throw new ArgumentException("The target branch was not found.", nameof(targetBranchName));
        }

        using var repository = await LovelyGitRepository
            .OpenObjectDatabaseAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        return await BuildResponseAsync(
            repository,
            currentHash,
            target.Value,
            currentName,
            targetName,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<GitObjectId?> ResolveTargetAsync(
        string gitDirectory,
        string targetName,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var local = await GitHeadReader.ResolveRefAsync(
                gitDirectory,
                $"refs/heads/{targetName}",
                objectFormat,
                cancellationToken)
            .ConfigureAwait(false);
        return local ?? await GitHeadReader.ResolveRefAsync(
                gitDirectory,
                $"refs/remotes/{targetName}",
                objectFormat,
                cancellationToken)
            .ConfigureAwait(false);
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
