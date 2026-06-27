using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class GitIndexReaderTests
{
    [Fact]
    public async Task ReadSnapshotAsync_ReadsCacheTreeRootId()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-index-");
        await RunGitAsync(directory.Path, "init");
        await RunGitAsync(directory.Path, "config", "user.email", "test@example.com");
        await RunGitAsync(directory.Path, "config", "user.name", "Test User");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "file.txt"), "hello");
        await RunGitAsync(directory.Path, "add", ".");
        await RunGitAsync(directory.Path, "commit", "-m", "initial");
        await RunGitAsync(directory.Path, "update-index", "--refresh");
        var expectedTree = (await RunGitAsync(directory.Path, "rev-parse", "HEAD^{tree}")).Trim();

        var snapshot = await new GitIndexReader().ReadSnapshotAsync(
            Path.Combine(directory.Path, ".git"),
            GitObjectFormat.Sha1,
            CancellationToken.None);

        Assert.Equal(expectedTree, snapshot.RootTreeId?.Value);
    }

    private static async Task<string> RunGitAsync(string workingDirectory, params string[] arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        }.WithArguments(arguments)) ?? throw new InvalidOperationException("Could not start git.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.True(process.ExitCode == 0, error);
        return output;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryDirectory(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public static TemporaryDirectory Create(string prefix)
        {
            return new TemporaryDirectory(Directory.CreateTempSubdirectory(prefix));
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes &= ~FileAttributes.ReadOnly;
            }

            _directory.Delete(recursive: true);
        }
    }
}

internal static class ProcessStartInfoExtensions
{
    public static ProcessStartInfo WithArguments(this ProcessStartInfo startInfo, string[] arguments)
    {
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
