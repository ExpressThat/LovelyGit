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
        bool prune,
        CancellationToken cancellationToken)
    {
        return RunRemoteCommandAsync(
            repositoryPath,
            BuildFetchArguments(remoteName, prune),
            cancellationToken);
    }

    internal static IReadOnlyList<string> BuildFetchArguments(string? remoteName, bool prune)
    {
        IReadOnlyList<string> arguments = string.IsNullOrWhiteSpace(remoteName)
            ? ["fetch", "--all"]
            : ["fetch"];
        if (prune)
        {
            arguments = arguments.Concat(["--prune"]).ToArray();
        }
        return AddRemote(arguments, remoteName);
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
        GitPushMode mode,
        string? remoteName,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> arguments = mode == GitPushMode.ForceWithLease
            ? ["push", "--force-with-lease"]
            : ["push"];
        return RunRemoteCommandAsync(
            repositoryPath,
            AddRemote(arguments, remoteName),
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

        var message = BestErrorLine(result.StandardError)
            ?? BestErrorLine(result.StandardOutput)
            ?? "Git remote command failed.";
        throw new InvalidOperationException(message);
    }

    private static string? BestErrorLine(string text)
    {
        string? fallback = null;
        foreach (var line in text.AsSpan().EnumerateLines())
        {
            var trimmed = line.Trim();
            if (trimmed.IsEmpty)
            {
                continue;
            }

            fallback ??= trimmed.ToString();
            if (trimmed.Contains("rejected", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("fatal:", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("error:", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.ToString();
            }
        }

        return fallback;
    }
}
