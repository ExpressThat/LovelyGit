using CliWrap;
using CliWrap.Buffered;
using CliWrap.Builders;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed partial class GitCliService
{
    private readonly Lazy<GitCliInstallation> _installation = new(ResolveInstallation);

    public GitCliInstallation Installation => _installation.Value;

    public Command CreateCommand(
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        bool validateExitCode = true,
        IReadOnlyDictionary<string, string?>? environmentVariables = null)
    {
        var installation = Installation;
        var command = global::CliWrap.Cli.Wrap(installation.GitExecutablePath)
            .WithArguments(arguments)
            .WithEnvironmentVariables(environment =>
                ConfigureEnvironment(environment, installation, environmentVariables));

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            command = command.WithWorkingDirectory(workingDirectory);
        }

        if (!validateExitCode)
        {
            command = command.WithValidation(CommandResultValidation.None);
        }

        return command;
    }

    public Task<BufferedCommandResult> ExecuteBufferedAsync(
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        bool validateExitCode = true,
        CancellationToken cancellationToken = default)
    {
        return CreateCommand(arguments, workingDirectory, validateExitCode)
            .ExecuteBufferedAsync(cancellationToken);
    }

    private static void ConfigureEnvironment(
        EnvironmentVariablesBuilder environment,
        GitCliInstallation installation,
        IReadOnlyDictionary<string, string?>? additionalVariables)
    {
        environment.Set("GIT_TERMINAL_PROMPT", "0");
        environment.Set("PATH", BuildPathValue(installation.PathDirectories));
        if (additionalVariables == null)
        {
            return;
        }

        foreach (var (name, value) in additionalVariables)
        {
            environment.Set(name, value);
        }
    }

    private static string BuildPathValue(IReadOnlyList<string> pathDirectories)
    {
        var existingPath = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(existingPath))
        {
            return string.Join(Path.PathSeparator, pathDirectories);
        }

        return string.Join(Path.PathSeparator, pathDirectories) + Path.PathSeparator + existingPath;
    }

}
