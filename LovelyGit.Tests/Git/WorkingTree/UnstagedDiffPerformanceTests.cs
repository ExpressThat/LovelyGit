using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.WorkingTree;

[Collection(PerformanceTestCollection.Name)]
public sealed class UnstagedDiffPerformanceTests(ITestOutputHelper output)
{
    private const int RefCountPerKind = 500;
    private const int LineCount = 20_000;
    private static readonly RepositoryTemplate<bool> Template = new(
        "lovelygit-unstaged-diff-template-",
        InitializeTemplate, prewarmCopies: 1);

    [Fact]
    public async Task AlternatingLargeDiffViews_DoNotLoadRepositoryRefs()
    {
        var (directory, _) = Template.CreateCopy("lovelygit-unstaged-diff-");
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
                combined < TimeSpan.FromMilliseconds(250),
                $"Combined view switches took {combined}.");
            Assert.True(
                sideBySide < TimeSpan.FromMilliseconds(250),
                $"Side-by-side view switches took {sideBySide}.");
            Assert.True(allocated < 85_000_000, $"View switches allocated {allocated:N0} bytes.");
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
        Assert.Equal("Modified", response.Status);
        Assert.True(response.HasDifferences);
        return elapsed;
    }

    private static Task<CommitFileDiffResponse> ReadAsync(
        WorkingTreeChangeService service,
        DirectoryInfo directory,
        CommitDiffViewMode viewMode) =>
        service.GetFileDiffAsync(
            directory.FullName,
            "large.txt",
            WorkingTreeChangeGroup.Unstaged,
            viewMode,
            ignoreWhitespace: false,
            CancellationToken.None);

    private static bool InitializeTemplate(DirectoryInfo directory)
    {
        InitializedRepositoryTemplate.CopyInto(directory);
        var baseText = BuildText(modified: false);
        File.WriteAllText(Path.Combine(directory.FullName, "large.txt"), baseText);
        var git = new GitCliService();
        Run(git, directory, ["add", "large.txt"]);
        Run(git, directory, ["commit", "-m", "large base"]);
        var head = Run(git, directory, ["rev-parse", "HEAD"]).Trim();
        var gitDirectory = Path.Combine(directory.FullName, ".git");
        for (var index = 0; index < RefCountPerKind; index++)
        {
            WriteRef(gitDirectory, $"refs/heads/branch-{index}", head);
            WriteRef(gitDirectory, $"refs/remotes/origin/branch-{index}", head);
            WriteRef(gitDirectory, $"refs/tags/tag-{index}", head);
        }

        File.WriteAllText(Path.Combine(directory.FullName, "large.txt"), BuildText(modified: true));
        return true;
    }

    private static string BuildText(bool modified)
    {
        var text = new StringBuilder(400_000);
        for (var index = 0; index < LineCount; index++)
        {
            text.Append(modified && index % 100 == 0 ? "changed line " : "tracked line ")
                .Append(index).Append('\n');
        }
        return text.ToString();
    }

    private static string Run(
        GitCliService git,
        DirectoryInfo directory,
        IReadOnlyList<string> arguments) =>
        git.ExecuteBufferedAsync(arguments, directory.FullName)
            .GetAwaiter().GetResult().StandardOutput;

    private static void WriteRef(string gitDirectory, string name, string hash)
    {
        var path = Path.Combine(gitDirectory, name.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, hash + "\n");
    }

    private static void DeleteDirectory(DirectoryInfo directory)
        => RepositoryTemplateLifetime.DeleteDirectory(directory);
}
