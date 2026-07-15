using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Bisect;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.Bisect;

[Collection(PerformanceTestCollection.Name)]
public sealed class NativeGitBisectStateReaderPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ReadAsync_RefreshesLargeRefRepositoryWithinSessionBudget()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-bisect-performance-");
        try
        {
            InitializedRepositoryTemplate.CopyInto(directory, "master");
            var commit = await GitHeadReader.ResolveAsync(
                Path.Combine(directory.FullName, ".git"),
                Path.Combine(directory.FullName, ".git"),
                GitObjectFormat.Sha1,
                CancellationToken.None);
            Assert.True(commit.HasValue);
            SeedSession(directory.FullName, commit.Value.ToString(), 1_500);
            var reader = new NativeGitBisectStateReader();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            GitBisectState? state = null;
            for (var iteration = 0; iteration < 5; iteration++)
            {
                state = await reader.ReadAsync(directory.FullName, CancellationToken.None);
            }

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
            Assert.True(state!.IsActive);
            Assert.Equal(commit.Value.ToString(), state.BadCommit);
            Assert.Equal(commit.Value.ToString(), state.FirstBadCommit);
            Assert.Single(state.GoodCommits);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(100), $"Refreshes took {elapsed}.");
            Assert.True(allocated < 1_000_000, $"Refreshes allocated {allocated:N0} bytes.");
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

    [Fact]
    public async Task StartAsync_DoesNotScaleWithUnrelatedRefs()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-bisect-start-performance-");
        try
        {
            InitializedRepositoryTemplate.CopyInto(directory, "master");
            var git = new GitCliService();
            var good = await ReadHeadAsync(git, directory.FullName);
            await git.ExecuteBufferedAsync(
                ["commit", "--allow-empty", "-m", "Known bad"], directory.FullName);
            var bad = await ReadHeadAsync(git, directory.FullName);
            SeedUnrelatedRefs(directory.FullName, bad, 1_500);
            var reader = new NativeGitBisectStateReader();
            var service = new GitBisectCommandService(new GitOperationService(git), reader);
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            var state = await service.ExecuteAsync(
                directory.FullName, GitBisectAction.Start, good, CancellationToken.None);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"StartElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
            Assert.True(state.IsActive);
            Assert.Equal(bad, state.BadCommit);
            Assert.Equal([good], state.GoodCommits);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(200), $"Start took {elapsed}.");
            Assert.True(allocated < 1_200_000, $"Start allocated {allocated:N0} bytes.");
        }
        finally
        {
            foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
                file.Attributes = FileAttributes.Normal;
            directory.Delete(recursive: true);
        }
    }

    private static void SeedSession(string repositoryPath, string commit, int extraRefCount)
    {
        var gitDirectory = Path.Combine(repositoryPath, ".git");
        var heads = Directory.CreateDirectory(Path.Combine(gitDirectory, "refs", "heads", "perf"));
        for (var index = 0; index < extraRefCount; index++)
        {
            File.WriteAllText(Path.Combine(heads.FullName, $"branch-{index:D4}"), commit + "\n");
        }
        var bisect = Directory.CreateDirectory(Path.Combine(gitDirectory, "refs", "bisect"));
        File.WriteAllText(Path.Combine(bisect.FullName, "bad"), commit + "\n");
        File.WriteAllText(Path.Combine(bisect.FullName, $"good-{commit}"), commit + "\n");
        File.WriteAllText(Path.Combine(gitDirectory, "BISECT_START"), "master\n");
        File.WriteAllText(
            Path.Combine(gitDirectory, "BISECT_LOG"),
            $"# first bad commit: [{commit}] subject\n");
    }

    private static void SeedUnrelatedRefs(string repositoryPath, string commit, int count)
    {
        var heads = Directory.CreateDirectory(
            Path.Combine(repositoryPath, ".git", "refs", "heads", "perf"));
        for (var index = 0; index < count; index++)
            File.WriteAllText(Path.Combine(heads.FullName, $"branch-{index:D4}"), commit + "\n");
    }

    private static async Task<string> ReadHeadAsync(GitCliService git, string path) =>
        (await git.ExecuteBufferedAsync(["rev-parse", "HEAD"], path)).StandardOutput.Trim();
}
