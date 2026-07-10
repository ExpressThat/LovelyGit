using ExpressThat.LovelyGit.Services.Git.Cli;

namespace ExpressThat.LovelyGit.Services.Git.Patches;

internal sealed class PatchApplyService
{
    private readonly GitCliService _gitCliService;

    public PatchApplyService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task ApplyAsync(
        string repositoryPath,
        string patchPath,
        bool stageChanges,
        bool reverse,
        CancellationToken cancellationToken)
    {
        var fullPatchPath = Path.GetFullPath(patchPath);
        if (!File.Exists(fullPatchPath))
        {
            throw new FileNotFoundException("The selected patch no longer exists.", fullPatchPath);
        }

        var arguments = BuildArguments(fullPatchPath, stageChanges, reverse, checkOnly: true);
        var check = await _gitCliService
            .ExecuteBufferedAsync(
                arguments,
                repositoryPath,
                validateExitCode: false,
                cancellationToken)
            .ConfigureAwait(false);
        if (check.ExitCode != 0)
        {
            throw new InvalidOperationException(FormatFailure(check.StandardError));
        }

        arguments = BuildArguments(fullPatchPath, stageChanges, reverse, checkOnly: false);
        var apply = await _gitCliService
            .ExecuteBufferedAsync(
                arguments,
                repositoryPath,
                validateExitCode: false,
                cancellationToken)
            .ConfigureAwait(false);
        if (apply.ExitCode != 0)
        {
            throw new InvalidOperationException(FormatFailure(apply.StandardError));
        }
    }

    internal static IReadOnlyList<string> BuildArguments(
        string patchPath,
        bool stageChanges,
        bool reverse,
        bool checkOnly)
    {
        var arguments = new List<string>(5) { "apply" };
        if (checkOnly) arguments.Add("--check");
        if (stageChanges) arguments.Add("--index");
        if (reverse) arguments.Add("--reverse");
        arguments.Add(patchPath);
        return arguments;
    }

    private static string FormatFailure(string standardError)
    {
        var message = standardError.Trim();
        return string.IsNullOrEmpty(message)
            ? "Git could not apply this patch to the current repository state."
            : message;
    }
}
