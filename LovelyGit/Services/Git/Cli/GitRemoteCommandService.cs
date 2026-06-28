using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed class GitRemoteCommandService
{
    private readonly GitCliService _gitCliService;

    public GitRemoteCommandService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public Task FetchAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        return RunRemoteCommandAsync(repositoryPath, ["fetch"], cancellationToken);
    }

    public Task PullAsync(
        string repositoryPath,
        GitPullMode mode,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> arguments = mode switch
        {
            GitPullMode.Rebase => ["pull", "--rebase"],
            GitPullMode.FastForwardOnly => ["pull", "--ff-only"],
            _ => ["pull"],
        };
        return RunRemoteCommandAsync(repositoryPath, arguments, cancellationToken);
    }

    public Task PushAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        return RunRemoteCommandAsync(repositoryPath, ["push"], cancellationToken);
    }

    private async Task RunRemoteCommandAsync(
        string repositoryPath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        var result = await _gitCliService
            .ExecuteBufferedAsync(
                arguments,
                repositoryPaths.WorkTreeDirectory,
                validateExitCode: false,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode == 0)
        {
            return;
        }

        var message = FirstNonEmptyLine(result.StandardError)
            ?? FirstNonEmptyLine(result.StandardOutput)
            ?? "Git remote command failed.";
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
