using CliWrap;
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

        cancellationToken.ThrowIfCancellationRequested();
        var arguments = BuildArguments(fullPatchPath, stageChanges, reverse);
        var errors = new PatchApplyErrorCollector();
        var apply = await _gitCliService
            .CreateCommand(arguments, repositoryPath, validateExitCode: false)
            .WithStandardErrorPipe(PipeTarget.ToDelegate(errors.Add))
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        if (apply.ExitCode != 0)
        {
            throw new InvalidOperationException(errors.FormatFailure());
        }
    }

    internal static IReadOnlyList<string> BuildArguments(
        string patchPath,
        bool stageChanges,
        bool reverse)
    {
        var arguments = new List<string>(4) { "apply" };
        if (stageChanges) arguments.Add("--index");
        if (reverse) arguments.Add("--reverse");
        arguments.Add(patchPath);
        return arguments;
    }

}
