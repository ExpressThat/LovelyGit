using System.Diagnostics;

namespace LovelyGit.Tests.Git.WorkingTree;

internal sealed class TemporaryDirectory : IDisposable
{
    private readonly DirectoryInfo _directory;

    private TemporaryDirectory(DirectoryInfo directory)
    {
        _directory = directory;
        Path = directory.FullName;
    }

    public string Path { get; }

    public static TemporaryDirectory Create(string prefix) =>
        new(Directory.CreateTempSubdirectory(prefix));

    public void Dispose()
    {
        foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes &= ~FileAttributes.ReadOnly;
        }

        _directory.Delete(recursive: true);
    }
}

internal static class GitTestProcess
{
    public static async Task<string> RunAsync(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start git.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.True(process.ExitCode == 0, error);
        return output;
    }
}
