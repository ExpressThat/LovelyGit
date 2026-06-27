using CliWrap.Buffered;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Checkout;

internal sealed class GitCheckoutCommandService
{
    private readonly GitCliService _gitCliService;

    public GitCheckoutCommandService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task CheckoutCommitDetachedAsync(
        string repositoryPath,
        string commitHash,
        CancellationToken cancellationToken)
    {
        if (!GitObjectId.TryParse(commitHash, out _))
        {
            throw new ArgumentException("Commit hash is not valid.", nameof(commitHash));
        }

        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        var result = await _gitCliService.ExecuteBufferedAsync(
            ["checkout", "--detach", commitHash],
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
            ?? "Git checkout command failed.";
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
