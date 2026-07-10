using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.TypeGeneration;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Git.Submodules;

internal sealed class SubmoduleCommandService
{
    private readonly GitCliService _gitCliService;

    public SubmoduleCommandService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task ExecuteAsync(
        string repositoryPath,
        string submodulePath,
        SubmoduleAction action,
        CancellationToken cancellationToken)
    {
        ValidatePath(repositoryPath, submodulePath);
        var arguments = action switch
        {
            SubmoduleAction.Initialize => new[] { "submodule", "update", "--init", "--recursive", "--", submodulePath },
            SubmoduleAction.Update => new[] { "submodule", "update", "--recursive", "--", submodulePath },
            SubmoduleAction.Synchronize => new[] { "submodule", "sync", "--recursive", "--", submodulePath },
            SubmoduleAction.Deinitialize => new[] { "submodule", "deinit", "-f", "--", submodulePath },
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };
        var result = await _gitCliService
            .ExecuteBufferedAsync(arguments, repositoryPath, validateExitCode: false, cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            var message = result.StandardError.Trim();
            throw new InvalidOperationException(
                string.IsNullOrEmpty(message) ? "Git could not update this submodule." : message);
        }
    }

    private static void ValidatePath(string repositoryPath, string submodulePath)
    {
        if (string.IsNullOrWhiteSpace(submodulePath) || Path.IsPathRooted(submodulePath))
        {
            throw new ArgumentException("A relative submodule path is required.", nameof(submodulePath));
        }

        var repositoryRoot = Path.GetFullPath(repositoryPath) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(repositoryPath, submodulePath));
        if (!candidate.StartsWith(repositoryRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The submodule path escapes the repository.", nameof(submodulePath));
        }
    }
}

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<SubmoduleAction>))]
public enum SubmoduleAction
{
    Initialize,
    Update,
    Synchronize,
    Deinitialize,
}
