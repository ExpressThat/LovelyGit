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
        var gitMessage = PreferredDiagnosticLine(operation.StandardError)
            ?? FirstNonEmptyLine(operation.StandardOutput)
            ?? $"{operation.OperationName} failed.";

        return operation.RecoveryHint is null
            ? gitMessage
            : $"{gitMessage} {operation.RecoveryHint}";
    }

    private static string? PreferredDiagnosticLine(string text)
    {
        var rejection = FindDiagnosticLine(text, rejectionOnly: true);
        if (rejection is not null)
        {
            return rejection;
        }

        return FindDiagnosticLine(text, rejectionOnly: false)
            ?? FirstNonEmptyLine(text);
    }

    private static string? FindDiagnosticLine(string text, bool rejectionOnly)
    {
        foreach (var line in text.AsSpan().EnumerateLines())
        {
            var trimmed = line.Trim();
            var isMatch = rejectionOnly
                ? trimmed.Contains("[rejected]", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Contains("[remote rejected]", StringComparison.OrdinalIgnoreCase)
                : trimmed.StartsWith("fatal:", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("error:", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("remote: error:", StringComparison.OrdinalIgnoreCase);
            if (!trimmed.IsEmpty && isMatch)
            {
                return trimmed.ToString();
            }
        }

        return null;
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
