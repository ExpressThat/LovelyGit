using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Submodules;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.Submodules;

[Collection(GitShellIntegrationCollection.Name)]
public sealed class NativeSubmoduleReaderPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ReadAsync_DoesNotScaleWithUnrelatedRefs()
    {
        var root = Directory.CreateTempSubdirectory("lovelygit-submodule-performance-");
        try
        {
            var git = new GitCliService();
            var child = Directory.CreateDirectory(Path.Combine(root.FullName, "child"));
            var parent = Directory.CreateDirectory(Path.Combine(root.FullName, "parent"));
            InitializedRepositoryTemplate.CopyInto(child, "master");
            InitializedRepositoryTemplate.CopyInto(parent, "master");
            await git.ExecuteBufferedAsync(
                ["-c", "protocol.file.allow=always", "submodule", "add", child.FullName, "deps/library"],
                parent.FullName);
            await git.ExecuteBufferedAsync(["commit", "-am", "Add submodule"], parent.FullName);
            var head = (await git.ExecuteBufferedAsync(["rev-parse", "HEAD"], parent.FullName))
                .StandardOutput.Trim();
            SeedUnrelatedRefs(parent.FullName, head, 1_500);
            GC.Collect();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            var response = await new NativeSubmoduleReader().ReadAsync(
                parent.FullName, CancellationToken.None);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
            var submodule = Assert.Single(response);
            Assert.Equal(SubmoduleState.Current, submodule.State);
            Assert.Equal(submodule.ExpectedCommit, submodule.CurrentCommit);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(100), $"Read took {elapsed}.");
            Assert.True(allocated < 1_000_000, $"Read allocated {allocated:N0} bytes.");
        }
        finally
        {
            foreach (var file in root.EnumerateFiles("*", SearchOption.AllDirectories))
                file.Attributes = FileAttributes.Normal;
            root.Delete(recursive: true);
        }
    }

    private static void SeedUnrelatedRefs(string repositoryPath, string commit, int count)
    {
        var heads = Directory.CreateDirectory(
            Path.Combine(repositoryPath, ".git", "refs", "heads", "perf"));
        for (var index = 0; index < count; index++)
            File.WriteAllText(Path.Combine(heads.FullName, $"branch-{index:D4}"), commit + "\n");
    }
}
