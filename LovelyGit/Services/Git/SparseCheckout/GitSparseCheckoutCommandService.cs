using ExpressThat.LovelyGit.Services.Git.Cli;

namespace ExpressThat.LovelyGit.Services.Git.SparseCheckout;

internal sealed class GitSparseCheckoutCommandService(
    GitCliService git,
    NativeSparseCheckoutReader reader)
{
    private const int MaximumPatterns = 500;
    private const int MaximumPatternLength = 4_096;

    public async Task<SparseCheckoutState> ExecuteAsync(
        string repositoryPath,
        SparseCheckoutAction action,
        bool coneMode,
        IReadOnlyList<string>? patterns,
        CancellationToken cancellationToken)
    {
        var arguments = BuildArguments(action, coneMode, patterns);
        var result = await git
            .ExecuteBufferedAsync(arguments, repositoryPath, false, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            var error = result.StandardError.Trim();
            if (error.Length == 0) error = result.StandardOutput.Trim();
            throw new InvalidOperationException(
                error.Length == 0 ? "Sparse checkout could not complete the operation." : error);
        }
        return await reader.ReadAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
    }

    internal static string[] BuildArguments(
        SparseCheckoutAction action,
        bool coneMode,
        IReadOnlyList<string>? patterns)
    {
        if (action == SparseCheckoutAction.Disable)
        {
            return ["sparse-checkout", "disable"];
        }

        if (action != SparseCheckoutAction.Set)
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }

        var normalized = NormalizePatterns(patterns, coneMode);
        return [
            "sparse-checkout",
            "set",
            coneMode ? "--cone" : "--no-cone",
            "--skip-checks",
            "--",
            .. normalized,
        ];
    }

    private static string[] NormalizePatterns(IReadOnlyList<string>? patterns, bool coneMode)
    {
        if (patterns == null || patterns.Count == 0 || patterns.Count > MaximumPatterns)
        {
            throw new ArgumentException($"Choose between 1 and {MaximumPatterns} sparse paths.");
        }

        var result = new List<string>(patterns.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in patterns)
        {
            var pattern = value.Trim().Replace('\\', '/');
            if (pattern.Length == 0 || pattern.Length > MaximumPatternLength ||
                pattern.IndexOfAny(['\0', '\r', '\n']) >= 0)
            {
                throw new ArgumentException("A sparse-checkout path or pattern is invalid.");
            }
            if (coneMode) pattern = ValidateConePath(pattern);
            if (seen.Add(pattern)) result.Add(pattern);
        }
        return result.ToArray();
    }

    private static string ValidateConePath(string pattern)
    {
        if (Path.IsPathRooted(pattern))
        {
            throw new ArgumentException("Cone-mode paths must be relative to the repository.");
        }

        pattern = pattern.Trim('/');
        if (pattern.Length == 0 || pattern.Split('/').Any(segment => segment is ".." or ".git"))
        {
            throw new ArgumentException("A cone-mode sparse path is invalid.");
        }
        return pattern;
    }
}
