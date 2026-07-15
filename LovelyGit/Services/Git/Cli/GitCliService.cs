using CliWrap;
using CliWrap.Buffered;
using CliWrap.Builders;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed partial class GitCliService
{
    private const string CheckoutWorkerCount = "checkout.workers=4";
    private const string ParallelCheckoutThreshold = "checkout.thresholdForParallelism=100";
    private readonly Lazy<GitCliInstallation> _installation = new(ResolveInstallation);
    private readonly IReadOnlyDictionary<string, string?>? _defaultEnvironmentVariables;

    public GitCliService()
    {
    }

    internal GitCliService(IReadOnlyDictionary<string, string?> defaultEnvironmentVariables)
    {
        _defaultEnvironmentVariables = defaultEnvironmentVariables;
    }

    public GitCliInstallation Installation => _installation.Value;

    public Command CreateCommand(
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        bool validateExitCode = true,
        IReadOnlyDictionary<string, string?>? environmentVariables = null)
    {
        var installation = Installation;
        var command = global::CliWrap.Cli.Wrap(installation.GitExecutablePath)
            .WithArguments(builder => builder
                .Add("-c")
                .Add(CheckoutWorkerCount)
                .Add("-c")
                .Add(ParallelCheckoutThreshold)
                .Add(arguments))
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

    private void ConfigureEnvironment(
        EnvironmentVariablesBuilder environment,
        GitCliInstallation installation,
        IReadOnlyDictionary<string, string?>? additionalVariables)
    {
        environment.Set("GIT_TERMINAL_PROMPT", "0");
        environment.Set("PATH", BuildPathValue(installation.PathDirectories));
        ApplyVariables(environment, _defaultEnvironmentVariables);
        ApplyVariables(environment, additionalVariables);
    }

    private static void ApplyVariables(
        EnvironmentVariablesBuilder environment,
        IReadOnlyDictionary<string, string?>? variables)
    {
        if (variables == null)
        {
            return;
        }

        foreach (var (name, value) in variables)
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
