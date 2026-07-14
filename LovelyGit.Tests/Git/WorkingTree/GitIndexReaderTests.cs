using System.Diagnostics;
using System.Buffers.Binary;
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

    [Fact]
    public async Task ReadEntriesForPathAsync_ReadsOnlyMatchingVersion4Stages()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-index-path-");
        await RunGitAsync(directory.Path, "init");
        await RunGitAsync(directory.Path, "config", "index.version", "4");
        var objectIds = new List<string>();
        for (var index = 0; index < 3; index++)
        {
            var objectPath = Path.Combine(directory.Path, $"object-{index}.txt");
            await File.WriteAllTextAsync(objectPath, $"version {index}");
            objectIds.Add((await RunGitAsync(directory.Path, "hash-object", "-w", objectPath)).Trim());
        }

        await RunGitWithInputAsync(
            directory.Path,
            $"100644 {objectIds[0]} 0\ta.txt\n" +
            $"100644 {objectIds[0]} 1\tmiddle.txt\n" +
            $"100644 {objectIds[1]} 2\tmiddle.txt\n" +
            $"100644 {objectIds[2]} 3\tmiddle.txt\n" +
            $"100644 {objectIds[0]} 0\tz.txt\n",
            "update-index", "--index-info");
        var gitDirectory = Path.Combine(directory.Path, ".git");
        var indexBytes = await File.ReadAllBytesAsync(Path.Combine(gitDirectory, "index"));
        Assert.Equal(4u, BinaryPrimitives.ReadUInt32BigEndian(indexBytes.AsSpan(4, 4)));

        var reader = new GitIndexReader();
        var entries = await reader.ReadEntriesForPathAsync(
            gitDirectory, GitObjectFormat.Sha1, "middle.txt", CancellationToken.None);
        var snapshot = await reader.ReadAsync(gitDirectory, GitObjectFormat.Sha1, CancellationToken.None);
        var status = await new GitIndexStatusScanner().ScanAsync(
            gitDirectory,
            directory.Path,
            GitObjectFormat.Sha1,
            CancellationToken.None,
            includeTrackedChanges: false);

        Assert.Equal([1, 2, 3], entries.Select(entry => entry.Stage));
        Assert.Equal(objectIds, entries.Select(entry => entry.ObjectId.Value));
        Assert.Equal(["a.txt", "middle.txt", "middle.txt", "middle.txt", "z.txt"], snapshot.Select(entry => entry.Path));
        Assert.Equal("middle.txt", Assert.Single(status.Response.Unmerged).Path);
        Assert.Equal(1, WorkingTreePreliminaryIndexReader.CountMissingRootEntries(
            Path.Combine(gitDirectory, "index"), ["a.txt", "missing.txt"], CancellationToken.None));
        Assert.Empty(await reader.ReadEntriesForPathAsync(
            gitDirectory, GitObjectFormat.Sha1, "missing.txt", CancellationToken.None));
        await Assert.ThrowsAsync<OperationCanceledException>(() => reader.ReadEntriesForPathAsync(
            gitDirectory, GitObjectFormat.Sha1, "middle.txt", new CancellationToken(canceled: true)));
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

    private static async Task RunGitWithInputAsync(
        string workingDirectory,
        string input,
        params string[] arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        }.WithArguments(arguments)) ?? throw new InvalidOperationException("Could not start git.");
        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.True(process.ExitCode == 0, error);
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
