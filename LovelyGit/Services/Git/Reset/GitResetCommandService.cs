using CliWrap.Buffered;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Reset;

namespace ExpressThat.LovelyGit.Services.Git.Reset;

internal sealed class GitResetCommandService
{
    private readonly GitCliService _gitCliService;

    public GitResetCommandService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task ResetCurrentBranchToCommitAsync(
        string repositoryPath,
        string commitHash,
        GitResetMode resetMode,
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
            ["reset", ResetModeArgument(resetMode), commitHash],
            repositoryPaths.WorkTreeDirectory,
            validateExitCode: false,
            cancellationToken).ConfigureAwait(false);

        ThrowIfFailed(result);
    }

    private static string ResetModeArgument(GitResetMode resetMode)
    {
        return resetMode switch
        {
            GitResetMode.Soft => "--soft",
            GitResetMode.Mixed => "--mixed",
            GitResetMode.Hard => "--hard",
            _ => throw new ArgumentOutOfRangeException(nameof(resetMode), resetMode, "Unknown reset mode."),
        };
    }

    private static void ThrowIfFailed(BufferedCommandResult result)
    {
        if (result.ExitCode == 0)
        {
            return;
        }

        var message = FirstNonEmptyLine(result.StandardError)
            ?? FirstNonEmptyLine(result.StandardOutput)
            ?? "Git reset command failed.";
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
