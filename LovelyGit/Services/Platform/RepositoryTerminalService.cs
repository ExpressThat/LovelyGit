using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Platform;

internal enum RepositoryTerminalPlatform
{
    Windows,
    MacOs,
    Linux,
}

internal sealed class RepositoryTerminalService
{
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(1);
    private readonly Func<ProcessStartInfo, Process?> _startProcess;

    public RepositoryTerminalService()
        : this(Process.Start)
    {
    }

    internal RepositoryTerminalService(Func<ProcessStartInfo, Process?> startProcess)
    {
        _startProcess = startProcess;
    }

    public async Task OpenAsync(string repositoryPath)
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

        var startTask = Task.Run(() => StartTerminal(fullPath));
        var completedTask = await Task.WhenAny(startTask, Task.Delay(StartTimeout))
            .ConfigureAwait(false);
        if (completedTask == startTask)
        {
            await startTask.ConfigureAwait(false);
            return;
        }

        _ = startTask.ContinueWith(
            task => _ = task.Exception,
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private void StartTerminal(string repositoryPath)
    {
        using var process = _startProcess(CreateTerminalStartInfo(repositoryPath));
        if (process == null)
        {
            throw new InvalidOperationException("The terminal could not be started.");
        }
    }

    internal static ProcessStartInfo CreateTerminalStartInfo(string repositoryPath)
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateTerminalStartInfo(repositoryPath, RepositoryTerminalPlatform.Windows);
        }

        if (OperatingSystem.IsMacOS())
        {
            return CreateTerminalStartInfo(repositoryPath, RepositoryTerminalPlatform.MacOs);
        }

        if (OperatingSystem.IsLinux())
        {
            return CreateTerminalStartInfo(repositoryPath, RepositoryTerminalPlatform.Linux);
        }

        throw new PlatformNotSupportedException("Opening a terminal is not supported on this platform.");
    }

    internal static ProcessStartInfo CreateTerminalStartInfo(
        string repositoryPath,
        RepositoryTerminalPlatform platform)
    {
        var startInfo = platform switch
        {
            RepositoryTerminalPlatform.Windows => new ProcessStartInfo("wt.exe"),
            RepositoryTerminalPlatform.MacOs => new ProcessStartInfo("open"),
            RepositoryTerminalPlatform.Linux => new ProcessStartInfo("x-terminal-emulator"),
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null),
        };

        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = false;
        startInfo.WorkingDirectory = repositoryPath;
        AddPlatformArguments(startInfo, repositoryPath, platform);
        return startInfo;
    }

    private static void AddPlatformArguments(
        ProcessStartInfo startInfo,
        string repositoryPath,
        RepositoryTerminalPlatform platform)
    {
        if (platform == RepositoryTerminalPlatform.Windows)
        {
            startInfo.ArgumentList.Add("--title");
            startInfo.ArgumentList.Add("LovelyGit Terminal");
            startInfo.ArgumentList.Add("-d");
            startInfo.ArgumentList.Add(repositoryPath);
            return;
        }

        if (platform == RepositoryTerminalPlatform.MacOs)
        {
            startInfo.ArgumentList.Add("-a");
            startInfo.ArgumentList.Add("Terminal");
            startInfo.ArgumentList.Add(repositoryPath);
        }
    }
}
