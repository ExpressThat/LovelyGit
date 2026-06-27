using CliWrap.Buffered;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Merge;

internal sealed class GitMergeCommandService
{
    private readonly GitCliService _gitCliService;

    public GitMergeCommandService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task MergeBranchIntoCurrentAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken)
    {
        if (!GitBranchNameValidator.IsValidBranchName(branchName))
        {
            throw new ArgumentException("Branch name is not valid.", nameof(branchName));
        }

        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        var result = await _gitCliService.ExecuteBufferedAsync(
            ["merge", branchName],
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
            ?? "Git merge command failed.";
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
