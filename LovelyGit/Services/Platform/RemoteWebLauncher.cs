using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Platform;

internal sealed class RemoteWebLauncher
{
    private readonly Func<ProcessStartInfo, Process?> _startProcess;

    public RemoteWebLauncher()
        : this(Process.Start)
    {
    }

    internal RemoteWebLauncher(Func<ProcessStartInfo, Process?> startProcess)
    {
        _startProcess = startProcess;
    }

    public void Open(string url)
    {
        var process = _startProcess(CreateStartInfo(url));
        if (process == null)
        {
            throw new InvalidOperationException("The default web browser could not be opened.");
        }

        process.Dispose();
    }

    internal static ProcessStartInfo CreateStartInfo(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Only secure web URLs can be opened.", nameof(url));
        }

        return new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true };
    }
}
