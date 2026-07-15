using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Lfs;

internal sealed class NativeGitLfsStateReader
{
    private readonly GitCliService _gitCliService;

    public NativeGitLfsStateReader(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task<LfsRepositoryState> ReadAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var patterns = await ReadPatternsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        return new LfsRepositoryState
        {
            IsAvailable = FindLfsExecutable(_gitCliService.Installation) != null,
            IsInitialized = await HasLfsPrePushHookAsync(
                    paths.GitDirectory,
                    cancellationToken)
                .ConfigureAwait(false),
            HasTrackedPatterns = patterns.Count > 0,
            TrackedPatterns = patterns,
        };
    }

    internal static string? FindLfsExecutable(GitCliInstallation installation)
    {
        var executableName = installation.OperatingSystem == GitCliOperatingSystem.Windows
            ? "git-lfs.exe"
            : "git-lfs";
        foreach (var directory in installation.PathDirectories)
        {
            var candidate = Path.Combine(directory, executableName);
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    internal static async Task<List<string>> ReadPatternsAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
        => await LfsAttributesReader.ReadAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

    private static async Task<bool> HasLfsPrePushHookAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var hookPath = Path.Combine(gitDirectory, "hooks", "pre-push");
        if (!File.Exists(hookPath)) return false;

        var hook = await File.ReadAllTextAsync(hookPath, cancellationToken)
            .ConfigureAwait(false);
        return hook.Contains("git lfs pre-push", StringComparison.OrdinalIgnoreCase);
    }

}
