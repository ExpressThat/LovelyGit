using CliWrap;
using CliWrap.Buffered;
using ExpressThat.LovelyGit.Services.Git.Cli;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.SparseCheckout;

internal sealed class GitSparseCheckoutCommandService(
    GitCliService git,
    NativeSparseCheckoutReader reader)
{
    private const int MaximumPatterns = 250_000;
    private const int MaximumPatternLength = 4_096;
    internal const int MaximumPatternTextLength = 64 * 1024 * 1024;

    public async Task<SparseCheckoutState> ExecuteAsync(
        string repositoryPath,
        SparseCheckoutAction action,
        bool coneMode,
        string? patternText,
        CancellationToken cancellationToken)
    {
        var arguments = BuildArguments(action, coneMode);
        BufferedCommandResult result;
        if (action == SparseCheckoutAction.Disable)
        {
            result = await git.ExecuteBufferedAsync(
                    arguments, repositoryPath, false, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var normalized = NormalizePatternText(patternText, coneMode);
            result = await git.CreateCommand(arguments, repositoryPath, false)
                .WithStandardInputPipe(PipeSource.FromString(normalized, Encoding.UTF8))
                .ExecuteBufferedAsync(cancellationToken)
                .ConfigureAwait(false);
        }
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
        bool coneMode)
    {
        if (action == SparseCheckoutAction.Disable)
        {
            return ["sparse-checkout", "disable"];
        }

        if (action != SparseCheckoutAction.Set)
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }

        return [
            "sparse-checkout",
            "set",
            coneMode ? "--cone" : "--no-cone",
            "--skip-checks",
            "--stdin",
        ];
    }

    internal static string NormalizePatternText(string? patternText, bool coneMode)
    {
        if (string.IsNullOrWhiteSpace(patternText) || patternText.Length > MaximumPatternTextLength)
        {
            throw new ArgumentException("The sparse-checkout specification is empty or too large.");
        }

        var result = new StringBuilder(patternText.Length + 1);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var count = 0;
        var start = 0;
        for (var index = 0; index <= patternText.Length; index++)
        {
            if (index < patternText.Length && patternText[index] != '\n') continue;
            var pattern = patternText[start..index].Trim().Replace('\\', '/');
            start = index + 1;
            if (pattern.Length == 0) continue;
            if (pattern.Length > MaximumPatternLength ||
                pattern.IndexOfAny(['\0', '\r', '\n']) >= 0)
            {
                throw new ArgumentException("A sparse-checkout path or pattern is invalid.");
            }
            if (coneMode) pattern = ValidateConePath(pattern);
            if (!seen.Add(pattern)) continue;
            if (++count > MaximumPatterns)
            {
                throw new ArgumentException($"Choose at most {MaximumPatterns:N0} sparse paths.");
            }
            result.Append(pattern).Append('\n');
        }
        if (count == 0) throw new ArgumentException("Choose at least one sparse path.");
        return result.ToString();
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
