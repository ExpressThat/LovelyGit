using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed partial class GitRemoteCommandService
{
    private readonly GitCliService _gitCliService;

    public GitRemoteCommandService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public Task FetchAsync(
        string repositoryPath,
        string? remoteName,
        CancellationToken cancellationToken)
    {
        return RunRemoteCommandAsync(
            repositoryPath,
            AddRemote(["fetch"], remoteName),
            cancellationToken);
    }

    public Task PullAsync(
        string repositoryPath,
        GitPullMode mode,
        string? remoteName,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> arguments = mode switch
        {
            GitPullMode.Rebase => ["pull", "--rebase"],
            GitPullMode.FastForwardOnly => ["pull", "--ff-only"],
            _ => ["pull"],
        };
        return RunRemoteCommandAsync(
            repositoryPath,
            AddRemote(arguments, remoteName),
            cancellationToken);
    }

    public Task PushAsync(
        string repositoryPath,
        string? remoteName,
        CancellationToken cancellationToken)
    {
        return RunRemoteCommandAsync(
            repositoryPath,
            AddRemote(["push"], remoteName),
            cancellationToken);
    }

    internal static IReadOnlyList<string> AddRemote(
        IReadOnlyList<string> arguments,
        string? remoteName)
    {
        if (string.IsNullOrWhiteSpace(remoteName))
        {
            return arguments;
        }

        if (!GitRemoteNameValidator.IsValidRemoteName(remoteName))
        {
            throw new ArgumentException("Remote name is not valid.", nameof(remoteName));
        }

        return arguments.Concat([remoteName.Trim()]).ToArray();
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
