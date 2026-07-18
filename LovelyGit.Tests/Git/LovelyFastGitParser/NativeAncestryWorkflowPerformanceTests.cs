using System.Diagnostics;
using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.FileBlame;
using ExpressThat.LovelyGit.Services.Git.FileHistory;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

[Collection(PerformanceTestCollection.Name)]
public sealed class NativeAncestryWorkflowPerformanceTests(ITestOutputHelper output)
{
    private const int HistoryLength = 5_000;
    private static readonly RepositoryTemplate<string> Template = new(
        "lovelygit-ancestry-workflow-template-",
        InitializeTemplate, prewarmCopies: 2);

    [Fact]
    public async Task FileHistory_DeepUnchangedPathRemainsBounded()
    {
        var (directory, head) = Template.CreateCopy("lovelygit-file-history-performance-");
        try
        {
            GC.Collect();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            var response = await NativeFileHistoryReader.ReadAsync(
                directory.FullName,
                "target.txt",
                head,
                100,
                HistoryLength + 10,
                Timeout.InfiniteTimeSpan,
                CancellationToken.None);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"FileHistoryMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
            Assert.Equal(HistoryLength + 1, response.ScannedCommitCount);
            Assert.Equal(FileHistoryChangeKind.Added, Assert.Single(response.Results).ChangeKind);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(700), $"History took {elapsed}.");
            Assert.True(allocated < 25_000_000, $"History allocated {allocated:N0} bytes.");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task FileBlame_DeepUnchangedPathRemainsBounded()
    {
        var (directory, head) = Template.CreateCopy("lovelygit-file-blame-performance-");
        try
        {
            GC.Collect();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            var response = await NativeFileBlameReader.ReadAsync(
                directory.FullName,
                "target.txt",
                head,
                HistoryLength + 10,
                Timeout.InfiniteTimeSpan,
                CancellationToken.None);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"FileBlameMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
            Assert.Equal(HistoryLength + 1, response.ScannedCommitCount);
            Assert.Equal(1, response.ResolvedLineCount);
            Assert.Single(response.Hunks);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(550), $"Blame took {elapsed}.");
            Assert.True(allocated < 30_000_000, $"Blame allocated {allocated:N0} bytes.");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static string InitializeTemplate(DirectoryInfo directory)
    {
        var git = new GitCliService();
        Run(git, directory, ["init", "--initial-branch", "main"]);
        git.CreateCommand(["fast-import", "--quiet"], directory.FullName)
            .WithStandardInputPipe(PipeSource.FromString(BuildFastImport(), Encoding.UTF8))
            .ExecuteAsync().GetAwaiter().GetResult();
        Run(git, directory, ["gc", "--prune=now"]);
        var head = Run(git, directory, ["rev-parse", "HEAD"]).Trim();
        SeedUnrelatedRefs(directory.FullName, head, 1_500);
        return head;
    }

    private static string BuildFastImport()
    {
        var import = new StringBuilder(1_000_000);
        AppendCommit(import, 1, null, includeTarget: true);
        for (var index = 0; index < HistoryLength; index++)
            AppendCommit(import, index + 2, index + 1, includeTarget: false);
        return import.AppendLine("done").ToString().ReplaceLineEndings("\n");
    }

    private static void AppendCommit(
        StringBuilder import,
        int mark,
        int? parent,
        bool includeTarget)
    {
        var value = $"noise-{mark}";
        import.AppendLine("commit refs/heads/main")
            .Append("mark :").AppendLine(mark.ToString())
            .Append("author LovelyGit Test <test@example.invalid> ")
            .Append(1_700_000_000 + mark).AppendLine(" +0000")
            .Append("committer LovelyGit Test <test@example.invalid> ")
            .Append(1_700_000_000 + mark).AppendLine(" +0000")
            .Append("data ").AppendLine(value.Length.ToString()).AppendLine(value);
        if (parent.HasValue) import.Append("from :").AppendLine(parent.Value.ToString());
        import.AppendLine("M 100644 inline noise.txt")
            .Append("data ").AppendLine((value.Length + 1).ToString()).AppendLine(value);
        if (includeTarget)
            import.AppendLine("M 100644 inline target.txt").AppendLine("data 7").AppendLine("target");
        import.AppendLine();
    }

    private static string Run(
        GitCliService git,
        DirectoryInfo directory,
        IReadOnlyList<string> arguments) => git.ExecuteBufferedAsync(
            arguments, directory.FullName).GetAwaiter().GetResult().StandardOutput;

    private static void SeedUnrelatedRefs(string repositoryPath, string commit, int count)
        => PackedRefFixture.AddBranches(repositoryPath, commit, count);

    private static void DeleteDirectory(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
            file.Attributes = FileAttributes.Normal;
        directory.Delete(recursive: true);
    }
}
