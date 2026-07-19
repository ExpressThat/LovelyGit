using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Worktrees;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.CommitGraph;

[Collection(PerformanceTestCollection.Name)]
public sealed class RepositoryRefsPerformanceTests(ITestOutputHelper output)
{
    private const int RefCountPerKind = 200;
    private const int WorktreeCount = 200;
    private static readonly RepositoryTemplate<bool> Template = new(
        "lovelygit-refs-refresh-template-",
        InitializeTemplate, prewarmCopies: 2);

    [Fact]
    public async Task LargeRepositoryMetadataRefresh_RemainsResponsive()
    {
        var (directory, _) = Template.CreateCopy("lovelygit-refs-refresh-");
        try
        {
            await RepositoryRefsService.ReadAsync(directory.FullName, CancellationToken.None);
            GC.Collect();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();
            for (var iteration = 0; iteration < 10; iteration++)
            {
                var response = await RepositoryRefsService.ReadAsync(
                    directory.FullName, CancellationToken.None);
                Assert.Equal("main", response.CurrentBranchName);
                Assert.Equal(WorktreeCount + 1, response.Worktrees.Count);
                Assert.Equal(WorktreeCount / 2, response.Worktrees.Count(item => item.IsLocked));
                Assert.Equal((RefCountPerKind * 3) + 1, response.Refs.Count);
            }

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}");
            output.WriteLine($"AllocatedBytes={allocated:N0}");
            Assert.True(elapsed < TimeSpan.FromMilliseconds(800), $"Refreshes took {elapsed}.");
            Assert.True(allocated < 15_000_000, $"Refreshes allocated {allocated:N0} bytes.");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task LargeWorktreeMetadataRead_RemainsResponsive()
    {
        var (directory, _) = Template.CreateCopy("lovelygit-worktree-read-");
        try
        {
            var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
                directory.FullName, CancellationToken.None);
            await GitWorktreeReader.ReadAsync(
                paths.WorktreeGitDirectory, paths.WorkTreeDirectory, CancellationToken.None);
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            IReadOnlyList<GitWorktree>? worktrees = null;
            for (var iteration = 0; iteration < 10; iteration++)
            {
                worktrees = await GitWorktreeReader.ReadAsync(
                    paths.WorktreeGitDirectory, paths.WorkTreeDirectory, CancellationToken.None);
            }

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"WorktreesElapsedMs={elapsed.TotalMilliseconds:F2}");
            output.WriteLine($"WorktreesAllocatedBytes={allocated:N0}");
            Assert.Equal(WorktreeCount + 1, worktrees!.Count);
            Assert.Equal(WorktreeCount / 2, worktrees.Count(item => item.IsLocked));
            Assert.True(elapsed < TimeSpan.FromMilliseconds(400), $"Reads took {elapsed}.");
            Assert.True(allocated < 5_000_000, $"Reads allocated {allocated:N0} bytes.");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static bool InitializeTemplate(DirectoryInfo directory)
    {
        var head = InitializedRepositoryTemplate.CopyInto(directory);
        var gitDirectory = Path.Combine(directory.FullName, ".git");
        for (var index = 0; index < RefCountPerKind; index++)
        {
            WriteRef(gitDirectory, $"refs/heads/branch-{index}", head);
            WriteRef(gitDirectory, $"refs/remotes/origin/branch-{index}", head);
            WriteRef(gitDirectory, $"refs/tags/tag-{index}", head);
        }

        var worktreesDirectory = Directory.CreateDirectory(Path.Combine(gitDirectory, "worktrees"));
        for (var index = 0; index < WorktreeCount; index++)
        {
            var admin = Directory.CreateDirectory(Path.Combine(worktreesDirectory.FullName, $"wt-{index}"));
            File.WriteAllText(Path.Combine(admin.FullName, "HEAD"), $"ref: refs/heads/branch-{index}\n");
            File.WriteAllText(
                Path.Combine(admin.FullName, "gitdir"),
                Path.Combine(directory.FullName, $"linked-{index}", ".git") + "\n");
            if ((index & 1) == 0)
            {
                File.WriteAllText(Path.Combine(admin.FullName, "locked"), "fixture\n");
            }
        }

        return true;
    }

    private static void WriteRef(string gitDirectory, string name, string hash)
    {
        var path = Path.Combine(gitDirectory, name.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, hash + "\n");
    }

    private static void DeleteDirectory(DirectoryInfo directory)
        => RepositoryTemplateLifetime.DeleteDirectory(directory);
}
