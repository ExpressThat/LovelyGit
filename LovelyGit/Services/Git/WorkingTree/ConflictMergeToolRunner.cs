using ExpressThat.LovelyGit.Services.Git.Cli;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal interface IConflictMergeToolRunner
{
    Task<ConflictMergeToolResult> RunAsync(
        string repositoryPath,
        string path,
        CancellationToken cancellationToken);
}

internal sealed record ConflictMergeToolResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);

internal sealed class GitConflictMergeToolRunner(GitCliService git) : IConflictMergeToolRunner
{
    public async Task<ConflictMergeToolResult> RunAsync(
        string repositoryPath,
        string path,
        CancellationToken cancellationToken)
    {
        var result = await git.ExecuteBufferedAsync(
            ["mergetool", "--no-prompt", "--", path],
            repositoryPath,
            validateExitCode: false,
            cancellationToken).ConfigureAwait(false);
        return new ConflictMergeToolResult(
            result.ExitCode,
            result.StandardOutput,
            result.StandardError);
    }
}
