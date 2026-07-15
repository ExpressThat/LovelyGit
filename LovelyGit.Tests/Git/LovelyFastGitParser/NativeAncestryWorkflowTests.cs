using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.FileBlame;
using ExpressThat.LovelyGit.Services.Git.FileHistory;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class NativeAncestryWorkflowTests
{
    [Fact]
    public async Task NullStart_UsesLinkedWorktreeHeadForHistoryAndBlame()
    {
        var repository = Directory.CreateTempSubdirectory("lovelygit-ancestry-linked-root-");
        var linkedPath = Path.Combine(Path.GetTempPath(), $"lovelygit-ancestry-linked-{Guid.NewGuid():N}");
        var git = new GitCliService();
        try
        {
            InitializedRepositoryTemplate.CopyInto(repository, "main");
            await File.WriteAllTextAsync(Path.Combine(repository.FullName, "linked.txt"), "main\n");
            await RunAsync(git, repository.FullName, "add", "linked.txt");
            await RunAsync(git, repository.FullName, "commit", "-m", "Main version");
            await RunAsync(git, repository.FullName, "worktree", "add", "-b", "feature", linkedPath);
            await File.WriteAllTextAsync(Path.Combine(linkedPath, "linked.txt"), "feature\n");
            await RunAsync(git, linkedPath, "add", "linked.txt");
            await RunAsync(git, linkedPath, "commit", "-m", "Feature version");
            var featureHead = (await RunAsync(git, linkedPath, "rev-parse", "HEAD")).Trim();

            var history = await NativeFileHistoryReader.ReadAsync(
                linkedPath, "linked.txt", null, 10, 100, Timeout.InfiniteTimeSpan, CancellationToken.None);
            var blame = await NativeFileBlameReader.ReadAsync(
                linkedPath, "linked.txt", null, 100, Timeout.InfiniteTimeSpan, CancellationToken.None);

            Assert.Equal(featureHead, history.Results[0].Hash);
            Assert.Equal("Feature version", history.Results[0].Subject);
            Assert.Equal(featureHead, Assert.Single(blame.Hunks).Hash);
        }
        finally
        {
            if (Directory.Exists(linkedPath))
            {
                await git.ExecuteBufferedAsync(
                    ["worktree", "remove", "--force", linkedPath], repository.FullName);
            }
            DeleteDirectory(repository);
            if (Directory.Exists(linkedPath)) DeleteDirectory(new DirectoryInfo(linkedPath));
        }
    }

    private static async Task<string> RunAsync(
        GitCliService git,
        string path,
        params string[] arguments) => (await git.ExecuteBufferedAsync(arguments, path)).StandardOutput;

    private static void DeleteDirectory(DirectoryInfo directory)
    {
        if (!directory.Exists) return;
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
            file.Attributes = FileAttributes.Normal;
        directory.Delete(recursive: true);
    }
}
