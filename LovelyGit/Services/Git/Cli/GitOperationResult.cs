namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed record GitOperationResult(
    string OperationName,
    string WorkingDirectory,
    IReadOnlyList<string> Arguments,
    string StandardOutput,
    string StandardError,
    int ExitCode,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    string? RecoveryHint)
{
    public TimeSpan Duration => EndedAt - StartedAt;

    public bool IsSuccess => ExitCode == 0;
}
