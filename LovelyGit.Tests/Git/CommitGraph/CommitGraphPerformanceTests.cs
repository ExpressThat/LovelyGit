using System.Diagnostics;
using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.CommitGraph;

[Collection(PerformanceTestCollection.Name)]
public sealed class CommitGraphPerformanceTests(ITestOutputHelper output)
{
    private const int CommitCount = 10_000;
    private static readonly RepositoryTemplate<int> Template = new(
        "lovelygit-graph-performance-template-",
        InitializeTemplate);

    [Fact]
    public async Task FirstPage_FromPackedHistory_HasBoundedLatencyAndAllocations()
    {
        var (directory, expectedCount) = Template.CreateCopy("lovelygit-graph-performance-");
        try
        {
            var repositoryId = Guid.NewGuid();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();
            var opened = await CommitGraphManager.TryOpenAsync(
                directory.FullName,
                repositoryId,
                null!,
                CancellationToken.None);
            var openElapsed = Stopwatch.GetElapsedTime(startedAt);
            using var graph = Assert.IsType<CommitGraphManager>(opened.Graph);

            startedAt = Stopwatch.GetTimestamp();
            var page = await graph.GetCommitGraphPageAsync(
                new CommitGraphCursorState(null, 0),
                128,
                CancellationToken.None);
            var pageElapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;

            output.WriteLine(
                $"{expectedCount:N0} packed commits: open {openElapsed.TotalMilliseconds:N1} ms; " +
                $"first page {pageElapsed.TotalMilliseconds:N1} ms; {allocated:N0} bytes");
            Assert.True(opened.Success, opened.Error);
            Assert.Equal(128, page.Response.Rows.Count);
            Assert.True(page.Response.HasMore);
            Assert.True(openElapsed < TimeSpan.FromMilliseconds(250), $"Open took {openElapsed}.");
            Assert.True(pageElapsed < TimeSpan.FromMilliseconds(500), $"First page took {pageElapsed}.");
            Assert.True(allocated < 16_000_000, $"First page allocated {allocated:N0} bytes.");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static int InitializeTemplate(DirectoryInfo directory)
    {
        var git = new GitCliService();
        Run(git, directory, ["init", "--initial-branch", "main"]);
        git.CreateCommand(["fast-import", "--quiet"], directory.FullName)
            .WithStandardInputPipe(PipeSource.FromString(BuildFastImport(), Encoding.UTF8))
            .ExecuteAsync()
            .GetAwaiter()
            .GetResult();
        Run(git, directory, ["gc", "--prune=now"]);
        return CommitCount;
    }

    private static string BuildFastImport()
    {
        var import = new StringBuilder(CommitCount * 180);
        for (var index = 0; index < CommitCount; index++)
        {
            var subject = $"Commit {index}";
            var contents = $"value-{index}\n";
            import.AppendLine("commit refs/heads/main")
                .Append("mark :").AppendLine((index + 1).ToString())
                .Append("author LovelyGit Test <test@example.invalid> ")
                .Append(1_700_000_000 + index).AppendLine(" +0000")
                .Append("committer LovelyGit Test <test@example.invalid> ")
                .Append(1_700_000_000 + index).AppendLine(" +0000")
                .Append("data ").AppendLine(Encoding.UTF8.GetByteCount(subject).ToString())
                .AppendLine(subject);
            if (index > 0)
            {
                import.Append("from :").AppendLine(index.ToString());
            }

            import.AppendLine("M 100644 inline history.txt")
                .Append("data ").AppendLine(Encoding.UTF8.GetByteCount(contents).ToString())
                .Append(contents).AppendLine();
        }

        return import.AppendLine("done").ToString().ReplaceLineEndings("\n");
    }

    private static void Run(
        GitCliService git,
        DirectoryInfo directory,
        IReadOnlyList<string> arguments) =>
        git.ExecuteBufferedAsync(arguments, directory.FullName).GetAwaiter().GetResult();

    private static void DeleteDirectory(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        directory.Delete(true);
    }
}
