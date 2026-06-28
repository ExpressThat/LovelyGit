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
}
