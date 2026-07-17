using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeProgressiveStatusTests
{
    private static readonly RepositoryTemplate<bool> WideRepositoryTemplate = new(
        "lovelygit-wide-status-template-",
        InitializeWideRepository);

    [Fact]
    public async Task TrackedOnly_ReturnsTrackedChangesWithoutUntrackedFiles()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-progressive-status-");
        await InitializeRepositoryAsync(directory.Path);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "tracked.txt"), "changed content");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "untracked.txt"), "new");

        var response = await new WorkingTreeStatusListService(new GitCliService())
            .GetChangesAsync(directory.Path, trackedOnly: true, CancellationToken.None);

        Assert.False(response.IsComplete);
        Assert.Equal("tracked.txt", Assert.Single(response.Unstaged).Path);
        Assert.Empty(response.Untracked);
    }

    [Fact]
    public async Task CompleteScan_ReturnsTheDeferredUntrackedFiles()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-progressive-status-");
        await InitializeRepositoryAsync(directory.Path);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "untracked.txt"), "new");

        var response = await new WorkingTreeStatusListService(new GitCliService())
            .GetChangesAsync(directory.Path, CancellationToken.None);

        Assert.True(response.IsComplete);
        Assert.Equal("untracked.txt", Assert.Single(response.Untracked).Path);
    }

    [Fact]
    public async Task TrackedOnly_WideRepositoryReturnsOneCompleteGitScan()
    {
        var (directory, _) = WideRepositoryTemplate.CreateCopy("lovelygit-wide-status-");
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory.FullName, "untracked.txt"), "new");

            var response = await new WorkingTreeStatusListService(new GitCliService())
                .GetChangesAsync(directory.FullName, trackedOnly: true, CancellationToken.None);

            Assert.True(response.IsComplete);
            Assert.Equal("untracked.txt", Assert.Single(response.Untracked).Path);
        }
        finally
        {
            foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            directory.Delete(recursive: true);
        }
    }

    private static async Task InitializeRepositoryAsync(string path)
    {
        await GitTestProcess.RunAsync(path, "init", "-b", "main");
        await GitTestProcess.RunAsync(path, "config", "user.email", "tests@lovelygit.local");
        await GitTestProcess.RunAsync(path, "config", "user.name", "LovelyGit Tests");
        await File.WriteAllTextAsync(Path.Combine(path, "tracked.txt"), "initial");
        await GitTestProcess.RunAsync(path, "add", "tracked.txt");
        await GitTestProcess.RunAsync(path, "commit", "-m", "initial");
    }

    private static bool InitializeWideRepository(DirectoryInfo directory)
    {
        var git = new GitCliService();
        git.ExecuteBufferedAsync(["init", "--initial-branch", "main"], directory.FullName)
            .GetAwaiter().GetResult();
        git.CreateCommand(["fast-import", "--quiet"], directory.FullName)
            .WithStandardInputPipe(PipeSource.FromString(BuildWideFastImport(), Encoding.UTF8))
            .ExecuteAsync().GetAwaiter().GetResult();
        git.ExecuteBufferedAsync(["reset", "--mixed", "main"], directory.FullName)
            .GetAwaiter().GetResult();
        return true;
    }

    private static string BuildWideFastImport()
    {
        var import = new StringBuilder(48_000)
            .AppendLine("blob").AppendLine("mark :1").AppendLine("data 7").AppendLine("content")
            .AppendLine("commit refs/heads/main").AppendLine("author Test <test@example.invalid> 1700000000 +0000")
            .AppendLine("committer Test <test@example.invalid> 1700000000 +0000")
            .AppendLine("data 4").AppendLine("wide");
        for (var index = 0; index <= WorkingTreeStatusScanPolicy.MaxTrackedEntriesForNativeDeepUntrackedScan; index++)
        {
            import.Append("M 100644 :1 file-").Append(index.ToString("D4")).AppendLine(".txt");
        }

        return import.AppendLine("done").ToString().ReplaceLineEndings("\n");
    }
}
