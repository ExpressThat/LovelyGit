using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Bisect;
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
}
