using ExpressThat.LovelyGit.Services.Git.Cli;

namespace ExpressThat.LovelyGit.Services.Git.Lfs;

internal sealed class GitLfsCommandService
{
    private const int MaximumPatternLength = 4_096;
    private readonly GitCliService _gitCliService;

    public GitLfsCommandService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task ExecuteAsync(
        string repositoryPath,
        GitLfsAction action,
        string? pattern,
        CancellationToken cancellationToken)
    {
        var arguments = BuildArguments(action, pattern);
        var result = await _gitCliService
            .ExecuteBufferedAsync(arguments, repositoryPath, false, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode == 0) return;

        var error = result.StandardError.Trim();
        if (string.IsNullOrEmpty(error)) error = result.StandardOutput.Trim();
        throw new InvalidOperationException(
            string.IsNullOrEmpty(error) ? "Git LFS could not complete the operation." : error);
    }

    internal static string[] BuildArguments(GitLfsAction action, string? pattern)
    {
        return action switch
        {
            GitLfsAction.Install => ["lfs", "install", "--local"],
            GitLfsAction.Track => ["lfs", "track", "--", ValidatePattern(pattern)],
            GitLfsAction.Untrack => ["lfs", "untrack", "--", ValidatePattern(pattern)],
            GitLfsAction.Fetch => ["lfs", "fetch"],
            GitLfsAction.Pull => ["lfs", "pull"],
            GitLfsAction.Prune => ["lfs", "prune"],
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };
    }

    private static string ValidatePattern(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("An LFS path pattern is required.", nameof(pattern));
        }

        if (pattern.Length > MaximumPatternLength || pattern.IndexOfAny(['\0', '\r', '\n']) >= 0)
        {
            throw new ArgumentException("The LFS path pattern is invalid.", nameof(pattern));
        }

        return pattern;
    }
}
