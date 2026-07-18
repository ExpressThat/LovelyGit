using System.Diagnostics;
using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using LovelyGit.Tests.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.CommitGraph;

[Collection(PerformanceTestCollection.Name)]
public sealed class CommitFileDiffPerformanceTests(ITestOutputHelper output)
{
    private const int LineCount = 5_000;
    private static readonly RepositoryTemplate<GitObjectId> Template = new(
        "lovelygit-file-diff-performance-template-",
        InitializeTemplate, prewarmCopies: 1);

    [Fact]
    public async Task LargeModifiedFile_HasResponsiveOpenAndViewSwitch()
    {
        var (directory, commitId) = Template.CreateCopy("lovelygit-file-diff-performance-");
        using var cacheDirectory = TemporaryDirectory.Create("lovelygit-file-diff-cache-");
        var databasePath = Path.Combine(cacheDirectory.Path, "cache.blite");
        GitRepoCacheDbContext.RegisterBsonKeys(databasePath);
        using var context = new GitRepoCacheDbContext(databasePath);
        var cache = new CommitGraphRepository(context);
        using var service = new CommitFileDiffService(cache);
        var repositoryId = Guid.NewGuid();
        try
        {
            SeedUnrelatedRefs(directory.FullName, commitId.ToString(), 1_500);
            GC.Collect();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();
            var side = await ReadAsync(service, repositoryId, directory, commitId, CommitDiffViewMode.SideBySide);
            var sideElapsed = Stopwatch.GetElapsedTime(startedAt);
            var openAllocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            startedAt = Stopwatch.GetTimestamp();
            var combined = await ReadAsync(service, repositoryId, directory, commitId, CommitDiffViewMode.Combined);
            var switchElapsed = Stopwatch.GetElapsedTime(startedAt);
            startedAt = Stopwatch.GetTimestamp();
            var repeated = await ReadAsync(service, repositoryId, directory, commitId, CommitDiffViewMode.SideBySide);
            var repeatedElapsed = Stopwatch.GetElapsedTime(startedAt);

            output.WriteLine(
                $"Persistence policy: side={CommitFileDiffCachingPolicy.ShouldPersist(side)}; " +
                $"combined={CommitFileDiffCachingPolicy.ShouldPersist(combined)}");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await WaitForCacheAsync(cache, repositoryId, commitId, CommitDiffViewMode.SideBySide, timeout.Token);
            await WaitForCacheAsync(cache, repositoryId, commitId, CommitDiffViewMode.Combined, timeout.Token);
            output.WriteLine(
                $"{LineCount:N0}-line file: open {sideElapsed.TotalMilliseconds:N1} ms; " +
                $"side-to-combined {switchElapsed.TotalMilliseconds:N1} ms; " +
                $"repeat {repeatedElapsed.TotalMilliseconds:N1} ms; " +
                $"open allocated {openAllocated:N0} bytes");

            Assert.True(side.HasDifferences);
            Assert.True(combined.HasDifferences);
            Assert.Equal(side.CommitHash, repeated.CommitHash);
            Assert.Equal(side.Path, repeated.Path);
            Assert.Equal(side.Status, repeated.Status);
            Assert.Equal(side.ViewMode, repeated.ViewMode);
            Assert.Equal(side.HasDifferences, repeated.HasDifferences);
            Assert.Equal(side.CompactLineSchema, repeated.CompactLineSchema);
            Assert.Equal(side.CompactLineCount, repeated.CompactLineCount);
            Assert.Equal(side.CompactLinesGzipBase64, repeated.CompactLinesGzipBase64);
            Assert.Equal(side.Lines, repeated.Lines);
            Assert.True(sideElapsed < TimeSpan.FromMilliseconds(600));
            Assert.True(switchElapsed < TimeSpan.FromMilliseconds(500));
            Assert.True(repeatedElapsed < TimeSpan.FromMilliseconds(100));
            Assert.True(openAllocated < 8_500_000);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static Task<CommitFileDiffResponse> ReadAsync(
        CommitFileDiffService service,
        Guid repositoryId,
        DirectoryInfo directory,
        GitObjectId commitId,
        CommitDiffViewMode viewMode) =>
        service.GetCommitFileDiffAsync(
            repositoryId,
            directory.FullName,
            commitId.ToString(),
            "large.txt",
            viewMode,
            ignoreWhitespace: false,
            CancellationToken.None);

    private static async Task WaitForCacheAsync(
        CommitGraphRepository cache,
        Guid repositoryId,
        GitObjectId commitId,
        CommitDiffViewMode viewMode,
        CancellationToken cancellationToken)
    {
        while (!await cache.HasCommitFileDiffAsync(
                   repositoryId,
                   commitId.ToString(),
                   "large.txt",
                   viewMode,
                   ignoreWhitespace: false,
                   cancellationToken))
        {
            await Task.Delay(5, cancellationToken);
        }
    }

    private static GitObjectId InitializeTemplate(DirectoryInfo directory)
    {
        new GitCliService().CreateCommand(["init", "--initial-branch", "main"], directory.FullName)
            .ExecuteAsync().GetAwaiter().GetResult();
        new GitCliService().CreateCommand(["fast-import", "--quiet"], directory.FullName)
            .WithStandardInputPipe(PipeSource.FromString(BuildFastImport(), Encoding.UTF8))
            .ExecuteAsync().GetAwaiter().GetResult();
        var hash = Run(directory, ["rev-parse", "HEAD"]).StandardOutput.Trim();
        Run(directory, ["gc", "--prune=now"]);
        return GitObjectId.Parse(hash);
    }

    private static string BuildFastImport()
    {
        var before = BuildText(modified: false);
        var after = BuildText(modified: true);
        var import = new StringBuilder(before.Length + after.Length + 500);
        AppendBlob(import, 1, before);
        AppendCommit(import, 3, null, 1, "Base large file");
        AppendBlob(import, 2, after);
        AppendCommit(import, 4, 3, 2, "Modify large file");
        return import.AppendLine("done").ToString().ReplaceLineEndings("\n");
    }

    private static string BuildText(bool modified)
    {
        var text = new StringBuilder(LineCount * 24);
        for (var index = 0; index < LineCount; index++)
        {
            text.Append("line ").Append(index.ToString("D5")).Append(' ')
                .Append(modified && index % 20 == 0 ? "changed" : "stable").Append('\n');
        }

        return text.ToString();
    }

    private static void AppendBlob(StringBuilder import, int mark, string text) =>
        import.AppendLine("blob").Append("mark :").AppendLine(mark.ToString())
            .Append("data ").AppendLine(Encoding.UTF8.GetByteCount(text).ToString()).Append(text);

    private static void AppendCommit(
        StringBuilder import,
        int mark,
        int? parent,
        int blob,
        string message)
    {
        import.AppendLine("commit refs/heads/main").Append("mark :").AppendLine(mark.ToString())
            .AppendLine("author LovelyGit Test <test@example.invalid> 1700000000 +0000")
            .AppendLine("committer LovelyGit Test <test@example.invalid> 1700000000 +0000")
            .Append("data ").AppendLine(message.Length.ToString()).AppendLine(message);
        if (parent.HasValue) import.Append("from :").AppendLine(parent.Value.ToString());
        import.Append("M 100644 :").Append(blob).AppendLine(" large.txt").AppendLine();
    }

    private static CliWrap.Buffered.BufferedCommandResult Run(
        DirectoryInfo directory,
        IReadOnlyList<string> arguments) =>
        new GitCliService().ExecuteBufferedAsync(arguments, directory.FullName).GetAwaiter().GetResult();

    private static void DeleteDirectory(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
            file.Attributes = FileAttributes.Normal;
        directory.Delete(true);
    }

    private static void SeedUnrelatedRefs(string repositoryPath, string commit, int count)
    {
        var heads = Directory.CreateDirectory(
            Path.Combine(repositoryPath, ".git", "refs", "heads", "perf"));
        for (var index = 0; index < count; index++)
        {
            File.WriteAllText(Path.Combine(heads.FullName, $"branch-{index:D4}"), commit + "\n");
        }
    }
}
