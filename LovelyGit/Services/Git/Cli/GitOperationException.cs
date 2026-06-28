namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed class GitOperationException : InvalidOperationException
{
    public GitOperationException(GitOperationResult operation)
        : base(CreateMessage(operation))
    {
        Operation = operation;
    }

    public GitOperationResult Operation { get; }

    private static string CreateMessage(GitOperationResult operation)
    {
        var gitMessage = FirstNonEmptyLine(operation.StandardError)
            ?? FirstNonEmptyLine(operation.StandardOutput)
            ?? $"{operation.OperationName} failed.";

        return operation.RecoveryHint is null
            ? gitMessage
            : $"{gitMessage} {operation.RecoveryHint}";
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
