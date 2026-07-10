namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed partial class GitCliService
{
    private static readonly string[] WindowsRelativeGitPaths =
    [
        Path.Combine("mingw64", "bin", "git.exe"),
        Path.Combine("cmd", "git.exe"),
        Path.Combine("bin", "git.exe"),
        "git.exe",
    ];

    private static readonly string[] UnixRelativeGitPaths =
    [
        Path.Combine("bin", "git"),
        Path.Combine("usr", "bin", "git"),
        "git",
    ];

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
            if (gitExecutablePath != null)
            {
                return CreateInstallation(operatingSystem, rootDirectory, gitExecutablePath);
            }
        }

        return ResolvePathGitInstallation(
                   operatingSystem,
                   Environment.GetEnvironmentVariable("PATH")) ??
               throw new FileNotFoundException(
                   $"Could not locate a packaged Git executable for {operatingSystem}.");
    }

    internal static GitCliInstallation? ResolvePathGitInstallation(
        GitCliOperatingSystem operatingSystem,
        string? pathValue)
    {
        var executableName = operatingSystem == GitCliOperatingSystem.Windows ? "git.exe" : "git";
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var directory in pathValue.Split(
                     Path.PathSeparator,
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(directory, executableName);
            if (!File.Exists(candidate))
            {
                continue;
            }

            var rootDirectory = GetPathInstallationRoot(candidate, operatingSystem);
            var gitExecutablePath = FindGitExecutable(rootDirectory, operatingSystem) ??
                                    Path.GetFullPath(candidate);
            return CreateInstallation(operatingSystem, rootDirectory, gitExecutablePath);
        }

        return null;
    }

    private static GitCliInstallation CreateInstallation(
        GitCliOperatingSystem operatingSystem,
        string rootDirectory,
        string gitExecutablePath) => new(
        operatingSystem,
        rootDirectory,
        gitExecutablePath,
        GetPathDirectories(rootDirectory, gitExecutablePath, operatingSystem));

    private static string? FindGitExecutable(
        string rootDirectory,
        GitCliOperatingSystem operatingSystem)
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
        return Directory.EnumerateFiles(rootDirectory, executableName, SearchOption.AllDirectories)
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

    private static string GetPathInstallationRoot(
        string gitExecutablePath,
        GitCliOperatingSystem operatingSystem)
    {
        var executableDirectory = Path.GetDirectoryName(Path.GetFullPath(gitExecutablePath)) ??
                                  AppContext.BaseDirectory;
        if (operatingSystem != GitCliOperatingSystem.Windows)
        {
            return executableDirectory;
        }

        var directory = new DirectoryInfo(executableDirectory);
        if (directory.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) &&
            directory.Parent?.Name.Equals("mingw64", StringComparison.OrdinalIgnoreCase) == true)
        {
            return directory.Parent.Parent?.FullName ?? executableDirectory;
        }

        if ((directory.Name.Equals("cmd", StringComparison.OrdinalIgnoreCase) ||
             directory.Name.Equals("bin", StringComparison.OrdinalIgnoreCase)) &&
            directory.Parent is { } parent &&
            File.Exists(Path.Combine(parent.FullName, "mingw64", "bin", "git.exe")))
        {
            return parent.FullName;
        }

        return executableDirectory;
    }

    private static GitCliOperatingSystem GetOperatingSystem()
    {
        if (OperatingSystem.IsWindows()) return GitCliOperatingSystem.Windows;
        if (OperatingSystem.IsMacOS()) return GitCliOperatingSystem.MacOs;
        if (OperatingSystem.IsLinux()) return GitCliOperatingSystem.Linux;
        throw new PlatformNotSupportedException("LovelyGit does not support this operating system.");
    }

    private static IEnumerable<string> GetCandidateRootDirectories()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Git");
        yield return Path.Combine(AppContext.BaseDirectory, "BundledTools", "Git");
        yield return Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "BundledTools", "Git");
    }
}
