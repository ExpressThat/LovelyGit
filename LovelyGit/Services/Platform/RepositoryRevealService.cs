using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Platform;

internal enum RepositoryRevealPlatform
{
    Windows,
    MacOs,
    Linux,
}

internal sealed class RepositoryRevealService
{
    private readonly Func<ProcessStartInfo, Process?> _startProcess;

    public RepositoryRevealService()
        : this(Process.Start)
    {
    }

    internal RepositoryRevealService(Func<ProcessStartInfo, Process?> startProcess)
    {
        _startProcess = startProcess;
    }

    public Task RevealAsync(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new ArgumentException("Repository path is required.", nameof(repositoryPath));
        }

        var fullPath = Path.GetFullPath(repositoryPath);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Repository folder was not found: {fullPath}");
        }

        using var process = _startProcess(CreateRevealStartInfo(fullPath));
        if (process == null)
        {
            throw new InvalidOperationException("The file manager could not be started.");
        }

        return Task.CompletedTask;
    }

    public Task RevealPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);
        var isFile = File.Exists(fullPath);
        var targetPath = isFile || Directory.Exists(fullPath)
            ? fullPath
            : Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(targetPath) || !Directory.Exists(targetPath) && !File.Exists(targetPath))
        {
            throw new FileNotFoundException($"Path was not found: {fullPath}", fullPath);
        }

        using var process = _startProcess(CreateRevealPathStartInfo(fullPath, targetPath, isFile));
        if (process == null)
        {
            throw new InvalidOperationException("The file manager could not be started.");
        }

        return Task.CompletedTask;
    }

    internal static ProcessStartInfo CreateRevealStartInfo(string repositoryPath)
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateRevealStartInfo(repositoryPath, RepositoryRevealPlatform.Windows);
        }

        if (OperatingSystem.IsMacOS())
        {
            return CreateRevealStartInfo(repositoryPath, RepositoryRevealPlatform.MacOs);
        }

        if (OperatingSystem.IsLinux())
        {
            return CreateRevealStartInfo(repositoryPath, RepositoryRevealPlatform.Linux);
        }

        throw new PlatformNotSupportedException("Opening a repository folder is not supported on this platform.");
    }

    internal static ProcessStartInfo CreateRevealStartInfo(
        string repositoryPath,
        RepositoryRevealPlatform platform)
    {
        var startInfo = platform switch
        {
            RepositoryRevealPlatform.Windows => new ProcessStartInfo("explorer.exe"),
            RepositoryRevealPlatform.MacOs => new ProcessStartInfo("open"),
            RepositoryRevealPlatform.Linux => new ProcessStartInfo("xdg-open"),
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null),
        };

        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.ArgumentList.Add(repositoryPath);

        return startInfo;
    }

    internal static ProcessStartInfo CreateRevealPathStartInfo(
        string requestedPath,
        string targetPath,
        RepositoryRevealPlatform platform,
        bool isFile = true)
    {
        var startInfo = platform switch
        {
            RepositoryRevealPlatform.Windows => new ProcessStartInfo("explorer.exe"),
            RepositoryRevealPlatform.MacOs => new ProcessStartInfo("open"),
            RepositoryRevealPlatform.Linux => new ProcessStartInfo("xdg-open"),
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null),
        };

        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        if (platform == RepositoryRevealPlatform.Windows && isFile)
        {
            startInfo.ArgumentList.Add($"/select,{requestedPath}");
        }
        else if (platform == RepositoryRevealPlatform.MacOs && isFile)
        {
            startInfo.ArgumentList.Add("-R");
            startInfo.ArgumentList.Add(requestedPath);
        }
        else
        {
            startInfo.ArgumentList.Add(targetPath);
        }

        return startInfo;
    }

    private static ProcessStartInfo CreateRevealPathStartInfo(
        string requestedPath,
        string targetPath,
        bool isFile)
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateRevealPathStartInfo(requestedPath, targetPath, RepositoryRevealPlatform.Windows, isFile);
        }

        if (OperatingSystem.IsMacOS())
        {
            return CreateRevealPathStartInfo(requestedPath, targetPath, RepositoryRevealPlatform.MacOs, isFile);
        }

        if (OperatingSystem.IsLinux())
        {
            return CreateRevealPathStartInfo(requestedPath, targetPath, RepositoryRevealPlatform.Linux, isFile);
        }

        throw new PlatformNotSupportedException("Revealing paths is not supported on this platform.");
    }
}
