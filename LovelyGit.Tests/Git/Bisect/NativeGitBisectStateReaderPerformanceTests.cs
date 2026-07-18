using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Bisect;
using ExpressThat.LovelyGit.Services.Git.Cli;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.Bisect;

[Collection(PerformanceTestCollection.Name)]
public sealed class NativeGitBisectStateReaderPerformanceTests(ITestOutputHelper output)
{
    private static readonly RepositoryTemplate<string> ReadTemplate = new(
        "lovelygit-bisect-read-template-",
        InitializeReadTemplate,
        prewarmCopies: 1);
    private static readonly RepositoryTemplate<StartState> StartTemplate = new(
        "lovelygit-bisect-start-template-",
        InitializeStartTemplate,
        prewarmCopies: 1);

    [Fact]
    public async Task ReadAsync_RefreshesLargeRefRepositoryWithinSessionBudget()
    {
        var (directory, commit) = ReadTemplate.CreateCopy("lovelygit-bisect-performance-");
        try
        {
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
            Assert.Equal(commit, state.BadCommit);
            Assert.Equal(commit, state.FirstBadCommit);
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
        var (directory, fixture) = StartTemplate.CreateCopy("lovelygit-bisect-start-performance-");
        try
        {
            var git = new GitCliService();
            var reader = new NativeGitBisectStateReader();
            var service = new GitBisectCommandService(new GitOperationService(git), reader);
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            var state = await service.ExecuteAsync(
                directory.FullName, GitBisectAction.Start, fixture.Good, CancellationToken.None);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"StartElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
            Assert.True(state.IsActive);
            Assert.Equal(fixture.Bad, state.BadCommit);
            Assert.Equal([fixture.Good], state.GoodCommits);
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

    private static string InitializeReadTemplate(DirectoryInfo directory)
    {
        InitializedRepositoryTemplate.CopyInto(directory, "master");
        var git = new GitCliService();
        var commit = ReadHeadAsync(git, directory.FullName).GetAwaiter().GetResult();
        SeedSession(directory.FullName, commit, 1_500);
        return commit;
    }

    private static StartState InitializeStartTemplate(DirectoryInfo directory)
    {
        InitializedRepositoryTemplate.CopyInto(directory, "master");
        var git = new GitCliService();
        var good = ReadHeadAsync(git, directory.FullName).GetAwaiter().GetResult();
        git.ExecuteBufferedAsync(["commit", "--allow-empty", "-m", "Known bad"], directory.FullName)
            .GetAwaiter().GetResult();
        var bad = ReadHeadAsync(git, directory.FullName).GetAwaiter().GetResult();
        SeedUnrelatedRefs(directory.FullName, bad, 1_500);
        return new StartState(good, bad);
    }

    private sealed record StartState(string Good, string Bad);
}
