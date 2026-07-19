using ExpressThat.LovelyGit.Services.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Configuration;

internal sealed class NativeGitCommitIdentityReader
{
    private readonly GitCliService? _gitCliService;

    public NativeGitCommitIdentityReader()
    {
    }

    public NativeGitCommitIdentityReader(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task<GitCommitIdentity> ReadAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        using var trace = LovelyGitTrace.Time("git.identity.read");
        return await ReadAsync(
            repositoryPath,
            GitIdentityReadOptions.CreateCurrent(GetWindowsGitRootDirectory()),
            cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task<GitCommitIdentity> ReadAsync(
        string repositoryPath,
        GitIdentityReadOptions options,
        CancellationToken cancellationToken)
    {
        GitRepositoryPaths paths;
        string? branchName;
        using (LovelyGitTrace.Time("git.identity.discovery"))
        {
            paths = await GitRepositoryDiscovery
                .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
                .ConfigureAwait(false);
            branchName = ReadBranchName(paths.WorktreeGitDirectory);
        }
        var parser = new GitIdentityConfigParser(
            paths.GitDirectory, branchName, options.HomeDirectory);
        var identity = new GitIdentityAccumulator();

        foreach (var path in options.SystemConfigPaths)
        {
            await parser.ReadAsync(
                path, GitIdentityValueSource.System, identity, cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var path in options.GlobalConfigPaths)
        {
            await parser.ReadAsync(
                path, GitIdentityValueSource.Global, identity, cancellationToken)
                .ConfigureAwait(false);
        }

        var worktreeConfigEnabled = await parser.ReadAsync(
            Path.Combine(paths.GitDirectory, "config"),
            GitIdentityValueSource.Repository,
            identity,
            cancellationToken,
            detectWorktreeConfig: true).ConfigureAwait(false);
        if (worktreeConfigEnabled)
        {
            await parser.ReadAsync(
                Path.Combine(paths.WorktreeGitDirectory, "config.worktree"),
                GitIdentityValueSource.Worktree,
                identity,
                cancellationToken).ConfigureAwait(false);
        }

        ApplyEnvironment(options, identity);
        return identity.Build();
    }

    private static void ApplyEnvironment(
        GitIdentityReadOptions options,
        GitIdentityAccumulator identity)
    {
        ApplyEnvironmentConfiguration(options, identity);
        var name = options.GetEnvironment("GIT_COMMITTER_NAME") ??
                   options.GetEnvironment("GIT_AUTHOR_NAME");
        var email = options.GetEnvironment("GIT_COMMITTER_EMAIL") ??
                    options.GetEnvironment("GIT_AUTHOR_EMAIL");
        if (!string.IsNullOrWhiteSpace(name))
        {
            identity.ApplyName(name, GitIdentityValueSource.Environment);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            identity.ApplyEmail(email, GitIdentityValueSource.Environment);
        }
    }

    private string? GetWindowsGitRootDirectory()
    {
        if (_gitCliService is null)
        {
            return null;
        }

        var installation = _gitCliService.Installation;
        return installation.OperatingSystem == GitCliOperatingSystem.Windows
            ? installation.RootDirectory
            : null;
    }

    private static void ApplyEnvironmentConfiguration(
        GitIdentityReadOptions options,
        GitIdentityAccumulator identity)
    {
        if (!int.TryParse(options.GetEnvironment("GIT_CONFIG_COUNT"), out var count) || count <= 0)
        {
            return;
        }

        for (var index = 0; index < count; index++)
        {
            var key = options.GetEnvironment($"GIT_CONFIG_KEY_{index}");
            var value = options.GetEnvironment($"GIT_CONFIG_VALUE_{index}");
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (key?.Equals("user.name", StringComparison.OrdinalIgnoreCase) == true)
            {
                identity.ApplyName(value, GitIdentityValueSource.Environment);
            }
            else if (key?.Equals("user.email", StringComparison.OrdinalIgnoreCase) == true)
            {
                identity.ApplyEmail(value, GitIdentityValueSource.Environment);
            }
        }
    }

    private static string? ReadBranchName(string worktreeGitDirectory)
    {
        var path = Path.Combine(worktreeGitDirectory, "HEAD");
        if (!File.Exists(path))
        {
            return null;
        }

        var value = File.ReadAllText(path).AsSpan().Trim();
        const string prefix = "ref: refs/heads/";
        return value.StartsWith(prefix, StringComparison.Ordinal)
            ? value[prefix.Length..].ToString()
            : null;
    }

}
