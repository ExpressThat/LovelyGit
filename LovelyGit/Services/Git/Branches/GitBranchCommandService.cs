using CliWrap.Buffered;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Branches;

internal sealed class GitBranchCommandService
{
    private readonly GitCliService _gitCliService;

    public GitBranchCommandService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task CreateBranchAsync(
        string repositoryPath,
        string branchName,
        string commitHash,
        CancellationToken cancellationToken)
    {
        if (!GitBranchNameValidator.IsValidBranchName(branchName))
        {
            throw new ArgumentException("Branch name is not valid.", nameof(branchName));
        }

        if (!GitObjectId.TryParse(commitHash, out _))
        {
            throw new ArgumentException("Commit hash is not valid.", nameof(commitHash));
        }

        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        var result = await _gitCliService.ExecuteBufferedAsync(
            ["branch", branchName, commitHash],
            repositoryPaths.WorkTreeDirectory,
            validateExitCode: false,
            cancellationToken).ConfigureAwait(false);

        ThrowIfFailed(result);
    }

    public async Task RenameBranchAsync(
        string repositoryPath,
        string oldBranchName,
        string newBranchName,
        CancellationToken cancellationToken)
    {
        if (!GitBranchNameValidator.IsValidBranchName(oldBranchName))
        {
            throw new ArgumentException("Branch name is not valid.", nameof(oldBranchName));
        }

        if (!GitBranchNameValidator.IsValidBranchName(newBranchName))
        {
            throw new ArgumentException("Branch name is not valid.", nameof(newBranchName));
        }

        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        var result = await _gitCliService.ExecuteBufferedAsync(
            ["branch", "-m", oldBranchName, newBranchName],
            repositoryPaths.WorkTreeDirectory,
            validateExitCode: false,
            cancellationToken).ConfigureAwait(false);

        ThrowIfFailed(result);
    }

    private static void ThrowIfFailed(BufferedCommandResult result)
    {
        if (result.ExitCode == 0)
        {
            return;
        }

        var message = FirstNonEmptyLine(result.StandardError)
            ?? FirstNonEmptyLine(result.StandardOutput)
            ?? "Git branch command failed.";
        throw new InvalidOperationException(message);
    }

    private static string? FirstNonEmptyLine(string text)
    {
        foreach (var line in text.AsSpan().EnumerateLines())
        {
            var trimmed = line.Trim();
            if (!trimmed.IsEmpty)
            {
                return trimmed.ToString();
            }
        }

        return null;
    }
}
