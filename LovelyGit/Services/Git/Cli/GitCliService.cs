using CliWrap;
using CliWrap.Buffered;
using CliWrap.Builders;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed class GitCliService
{
    private static readonly string[] WindowsRelativeGitPaths =
    [
        Path.Combine("cmd", "git.exe"),
        Path.Combine("bin", "git.exe"),
        Path.Combine("mingw64", "bin", "git.exe"),
        "git.exe",
    ];

    private static readonly string[] UnixRelativeGitPaths =
    [
        Path.Combine("bin", "git"),
        Path.Combine("usr", "bin", "git"),
        "git",
    ];

    private readonly Lazy<GitCliInstallation> _installation = new(ResolveInstallation);

    public GitCliInstallation Installation => _installation.Value;

    public Command CreateCommand(
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        bool validateExitCode = true)
    {
        var installation = Installation;
        var command = global::CliWrap.Cli.Wrap(installation.GitExecutablePath)
            .WithArguments(arguments)
            .WithEnvironmentVariables(environment => ConfigureEnvironment(environment, installation));

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
        GitCliInstallation installation)
    {
        environment.Set("GIT_TERMINAL_PROMPT", "0");
        environment.Set("PATH", BuildPathValue(installation.PathDirectories));
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

    private static GitCliInstallation ResolveInstallation()
    {
        var operatingSystem = GetOperatingSystem();
        foreach (var rootDirectory in GetCandidateRootDirectories())
        {
            if (!Directory.Exists(rootDirectory))
            {
                continue;
            }

            var gitExecutablePath = FindGitExecutable(rootDirectory, operatingSystem);
            if (gitExecutablePath == null)
            {
                continue;
            }

            return new GitCliInstallation(
                operatingSystem,
                rootDirectory,
                gitExecutablePath,
                GetPathDirectories(rootDirectory, gitExecutablePath, operatingSystem));
        }

        var pathGit = ResolvePathGitExecutable(operatingSystem);
        if (pathGit != null)
        {
            var gitDirectory = Path.GetDirectoryName(pathGit) ?? AppContext.BaseDirectory;
            return new GitCliInstallation(
                operatingSystem,
                gitDirectory,
                pathGit,
                [gitDirectory]);
        }

        throw new FileNotFoundException(
            $"Could not locate a packaged Git executable for {operatingSystem}. Expected it under '{Path.Combine(AppContext.BaseDirectory, "Git")}'.");
    }

    private static GitCliOperatingSystem GetOperatingSystem()
    {
        if (OperatingSystem.IsWindows())
        {
            return GitCliOperatingSystem.Windows;
        }

        if (OperatingSystem.IsMacOS())
        {
            return GitCliOperatingSystem.MacOs;
        }

        if (OperatingSystem.IsLinux())
        {
            return GitCliOperatingSystem.Linux;
        }

        throw new PlatformNotSupportedException("LovelyGit does not have a bundled Git layout for this operating system.");
    }

    private static IEnumerable<string> GetCandidateRootDirectories()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Git");
        yield return Path.Combine(AppContext.BaseDirectory, "BundledTools", "Git");
        yield return Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "BundledTools", "Git");
    }

    private static string? FindGitExecutable(string rootDirectory, GitCliOperatingSystem operatingSystem)
    {
        var candidates = operatingSystem == GitCliOperatingSystem.Windows
            ? WindowsRelativeGitPaths
            : UnixRelativeGitPaths;

        foreach (var candidate in candidates)
        {
            var path = Path.Combine(rootDirectory, candidate);
            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        var executableName = operatingSystem == GitCliOperatingSystem.Windows ? "git.exe" : "git";
        return Directory
            .EnumerateFiles(rootDirectory, executableName, SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .FirstOrDefault();
    }

    private static IReadOnlyList<string> GetPathDirectories(
        string rootDirectory,
        string gitExecutablePath,
        GitCliOperatingSystem operatingSystem)
    {
        var directories = new List<string>(4);
        AddDirectory(directories, Path.GetDirectoryName(gitExecutablePath));

        if (operatingSystem == GitCliOperatingSystem.Windows)
        {
            AddDirectory(directories, Path.Combine(rootDirectory, "cmd"));
            AddDirectory(directories, Path.Combine(rootDirectory, "mingw64", "bin"));
            AddDirectory(directories, Path.Combine(rootDirectory, "usr", "bin"));
        }
        else
        {
            AddDirectory(directories, Path.Combine(rootDirectory, "bin"));
            AddDirectory(directories, Path.Combine(rootDirectory, "usr", "bin"));
        }

        return directories;
    }

    private static void AddDirectory(List<string> directories, string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        var fullPath = Path.GetFullPath(directory);
        if (!directories.Contains(fullPath, StringComparer.OrdinalIgnoreCase))
        {
            directories.Add(fullPath);
        }
    }

    private static string? ResolvePathGitExecutable(GitCliOperatingSystem operatingSystem)
    {
        var executableName = operatingSystem == GitCliOperatingSystem.Windows ? "git.exe" : "git";
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(directory, executableName);
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }
}
