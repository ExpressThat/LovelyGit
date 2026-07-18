using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.CommitGraph;

[Collection(PerformanceTestCollection.Name)]
public sealed class CommitPatchPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task GetCommitPatchAsync_DoesNotScaleWithUnrelatedRefs()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-patch-performance-");
        try
        {
            InitializedRepositoryTemplate.CopyInto(directory, "master");
            var git = new GitCliService();
            await File.WriteAllTextAsync(Path.Combine(directory.FullName, "changed.txt"), "updated\n");
            await git.ExecuteBufferedAsync(["add", "changed.txt"], directory.FullName);
            await git.ExecuteBufferedAsync(["commit", "-m", "Update file"], directory.FullName);
            var head = (await git.ExecuteBufferedAsync(["rev-parse", "HEAD"], directory.FullName))
                .StandardOutput.Trim();
            SeedUnrelatedRefs(directory.FullName, head, 1_500);
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            var patch = await new CommitPatchService().GetCommitPatchAsync(
                directory.FullName, GitObjectId.Parse(head), CancellationToken.None);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
            Assert.Contains("changed.txt", patch.Patch, StringComparison.Ordinal);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(150), $"Patch took {elapsed}.");
            Assert.True(allocated < 1_000_000, $"Patch allocated {allocated:N0} bytes.");
        }
        finally
        {
            foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
                file.Attributes = FileAttributes.Normal;
            directory.Delete(recursive: true);
        }
    }

    private static void SeedUnrelatedRefs(string repositoryPath, string commit, int count)
        => PackedRefFixture.AddBranches(repositoryPath, commit, count);
}
