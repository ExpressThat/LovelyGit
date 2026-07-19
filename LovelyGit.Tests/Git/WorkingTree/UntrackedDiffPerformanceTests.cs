using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.WorkingTree;

[Collection(PerformanceTestCollection.Name)]
public sealed class UntrackedDiffPerformanceTests(ITestOutputHelper output)
{
    private const int RefCountPerKind = 500;
    private const int LineCount = 20_000;
    private static readonly RepositoryTemplate<bool> Template = new(
        "lovelygit-untracked-diff-template-",
        InitializeTemplate, prewarmCopies: 1);

    [Fact]
    public async Task AlternatingLargeDiffViews_DoNotLoadRepositoryRefs()
    {
        var (directory, _) = Template.CreateCopy("lovelygit-untracked-diff-");
        try
        {
            var service = new WorkingTreeChangeService();
            await ReadAsync(service, directory, CommitDiffViewMode.Combined);
            GC.Collect();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var combinedTicks = 0L;
            var sideBySideTicks = 0L;
            for (var iteration = 0; iteration < 5; iteration++)
            {
                combinedTicks += await MeasureAsync(
                    service, directory, CommitDiffViewMode.Combined);
                sideBySideTicks += await MeasureAsync(
                    service, directory, CommitDiffViewMode.SideBySide);
            }

            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            var combined = Stopwatch.GetElapsedTime(0, combinedTicks);
            var sideBySide = Stopwatch.GetElapsedTime(0, sideBySideTicks);
            output.WriteLine($"CombinedMs={combined.TotalMilliseconds:F2}");
            output.WriteLine($"SideBySideMs={sideBySide.TotalMilliseconds:F2}");
            output.WriteLine($"AllocatedBytes={allocated:N0}");
            Assert.True(
                combined < TimeSpan.FromMilliseconds(100),
                $"Combined view switches took {combined}.");
            Assert.True(
                sideBySide < TimeSpan.FromMilliseconds(100),
                $"Side-by-side view switches took {sideBySide}.");
            Assert.True(allocated < 16_000_000, $"View switches allocated {allocated:N0} bytes.");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static async Task<long> MeasureAsync(
        WorkingTreeChangeService service,
        DirectoryInfo directory,
        CommitDiffViewMode viewMode)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var response = await ReadAsync(service, directory, viewMode);
        var elapsed = Stopwatch.GetTimestamp() - startedAt;
        Assert.Equal(viewMode, response.ViewMode);
        Assert.True(response.HasDifferences);
        Assert.Equal(
            LineCount + 1,
            response.Lines.Count + response.CompactLineCount + response.VirtualLineCount);
        return elapsed;
    }

    private static Task<CommitFileDiffResponse> ReadAsync(
        WorkingTreeChangeService service,
        DirectoryInfo directory,
        CommitDiffViewMode viewMode) =>
        service.GetFileDiffAsync(
            directory.FullName,
            "large.txt",
            WorkingTreeChangeGroup.Untracked,
            viewMode,
            ignoreWhitespace: false,
            CancellationToken.None);

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

        var text = new StringBuilder(400_000);
        for (var index = 0; index < LineCount; index++)
        {
            text.Append("untracked line ").Append(index).Append('\n');
        }
        File.WriteAllText(Path.Combine(directory.FullName, "large.txt"), text.ToString());
        return true;
    }

    private static void WriteRef(string gitDirectory, string name, string hash)
    {
        var path = Path.Combine(gitDirectory, name.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, hash + "\n");
    }

    private static void DeleteDirectory(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        directory.Delete(true);
    }
}
