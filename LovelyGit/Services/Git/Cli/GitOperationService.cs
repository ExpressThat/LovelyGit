using CliWrap;
using CliWrap.Buffered;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed class GitOperationService
{
    private readonly GitCliService _gitCliService;

    public GitOperationService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task<GitOperationResult> ExecuteBufferedAsync(
        string operationName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        string? recoveryHint,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var result = await _gitCliService.ExecuteBufferedAsync(
            arguments,
            workingDirectory,
            validateExitCode: false,
            cancellationToken).ConfigureAwait(false);

        return CreateResult(
            operationName,
            arguments,
            workingDirectory,
            recoveryHint,
            startedAt,
            result);
    }

    public async Task<GitOperationResult> ExecuteRequiredBufferedAsync(
        string operationName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        string? recoveryHint,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteBufferedAsync(
            operationName,
            arguments,
            workingDirectory,
            recoveryHint,
            cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            throw new GitOperationException(result);
        }

        return result;
    }

    public async Task<GitOperationResult> ExecuteRequiredBufferedWithInputAsync(
        string operationName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        string? recoveryHint,
        PipeSource standardInput,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var output = await _gitCliService
            .CreateCommand(arguments, workingDirectory, validateExitCode: false)
            .WithStandardInputPipe(standardInput)
            .ExecuteBufferedAsync(cancellationToken)
            .ConfigureAwait(false);
        var result = CreateResult(
            operationName,
            arguments,
            workingDirectory,
            recoveryHint,
            startedAt,
            output);
        if (!result.IsSuccess)
        {
            throw new GitOperationException(result);
        }

        return result;
    }

    public async Task<GitOperationResult> ExecuteBufferedWithEnvironmentAsync(
        string operationName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        string? recoveryHint,
        IReadOnlyDictionary<string, string?> environmentVariables,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var result = await _gitCliService
            .CreateCommand(arguments, workingDirectory, validateExitCode: false, environmentVariables)
            .ExecuteBufferedAsync(cancellationToken)
            .ConfigureAwait(false);
        return CreateResult(
            operationName, arguments, workingDirectory, recoveryHint, startedAt, result);
    }

    private static GitOperationResult CreateResult(
        string operationName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        string? recoveryHint,
        DateTimeOffset startedAt,
        BufferedCommandResult result)
    {
        return new GitOperationResult(
            operationName,
            workingDirectory,
            arguments.ToArray(),
            result.StandardOutput,
            result.StandardError,
            result.ExitCode,
            startedAt,
            DateTimeOffset.UtcNow,
            recoveryHint);
    }
}
