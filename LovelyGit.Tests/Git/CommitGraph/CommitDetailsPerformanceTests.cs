using System.Diagnostics;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using LovelyGit.Tests.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitDetailsPerformanceTests(ITestOutputHelper output)
{
    private const int FileCount = 2_000;
    private static readonly RepositoryTemplate<GitObjectId> Template = new(
        "lovelygit-details-performance-template-",
        InitializeTemplate);

    [Fact]
    public async Task BuildManyModifiedFiles_HasBoundedLatency()
    {
        var (directory, commitId) = Template.CreateCopy("lovelygit-details-performance-");
        try
        {
            using var repository = await LovelyGitRepository.OpenAsync(
                directory.FullName,
                CancellationToken.None);
            var commit = await repository.GetCommitAsync(commitId, CancellationToken.None);
            var parent = await repository.GetCommitAsync(
                commit.ParentHashes[0],
                CancellationToken.None);
            var startedAt = Stopwatch.GetTimestamp();
            var comparison = await repository.GetChangedTreeFilesAsync(
                parent.TreeHash,
                commit.TreeHash,
                CancellationToken.None);
            var treeElapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            startedAt = Stopwatch.GetTimestamp();
            var files = await new CommitChangeDetector(new BlobLineAnalyzer(repository))
                .BuildChangedFilesAsync(
                    comparison.ParentFiles,
                    comparison.CurrentFiles,
                    CancellationToken.None);
            var blobsElapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
            output.WriteLine(
                $"{FileCount:N0} modified files: trees {treeElapsed.TotalMilliseconds:N1} ms; " +
                $"blobs {blobsElapsed.TotalMilliseconds:N1} ms; {allocated:N0} bytes");
            Assert.Equal(FileCount, files.Count);
            Assert.Equal((uint)FileCount, files.Aggregate(0u, (sum, file) => sum + file.Additions));
            Assert.Equal((uint)FileCount, files.Aggregate(0u, (sum, file) => sum + file.Deletions));
            Assert.True(
                treeElapsed + blobsElapsed < TimeSpan.FromMilliseconds(750),
                $"Commit details took {treeElapsed + blobsElapsed}.");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task InteractiveRead_DoesNotWaitForLargeCachePersistence()
    {
        var (directory, commitId) = Template.CreateCopy("lovelygit-details-interactive-");
        using var cacheDirectory = TemporaryDirectory.Create("lovelygit-details-interactive-cache-");
        var databasePath = Path.Combine(cacheDirectory.Path, "cache.blite");
        GitRepoCacheDbContext.RegisterBsonKeys(databasePath);
        using var context = new GitRepoCacheDbContext(databasePath);
        var cache = new CommitGraphRepository(context);
        var service = new CommitDetailsService(cache);
        var repositoryId = Guid.NewGuid();
        try
        {
            var startedAt = Stopwatch.GetTimestamp();
            var details = await service.GetCommitDetailsAsync(
                repositoryId,
                directory.FullName,
                commitId,
                0,
                CancellationToken.None);
            var responseElapsed = Stopwatch.GetElapsedTime(startedAt);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            while (!await cache.HasCommitDetailsAsync(
                       repositoryId,
                       commitId.ToString(),
                       timeout.Token))
            {
                await Task.Delay(5, timeout.Token);
            }

            var persistedElapsed = Stopwatch.GetElapsedTime(startedAt);
            output.WriteLine(
                $"Interactive 2,000 files: response {responseElapsed.TotalMilliseconds:N1} ms; " +
                $"persisted {persistedElapsed.TotalMilliseconds:N1} ms");
            Assert.Equal(FileCount, details.ChangedFiles.Count);
            Assert.True(responseElapsed < persistedElapsed);
            Assert.True(responseElapsed < TimeSpan.FromMilliseconds(750));
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static GitObjectId InitializeTemplate(DirectoryInfo directory)
    {
        Run(directory, ["init", "--initial-branch", "main"]);
        var git = new GitCliService();
        git.CreateCommand(["fast-import", "--quiet"], directory.FullName)
            .WithStandardInputPipe(PipeSource.FromString(BuildFastImport(), Encoding.UTF8))
            .ExecuteBufferedAsync()
            .GetAwaiter().GetResult();
        var hash = Run(directory, ["rev-parse", "HEAD"]).StandardOutput.Trim();
        Run(directory, ["gc", "--prune=now"]);
        return GitObjectId.Parse(hash, GitObjectFormat.Sha1);
    }

    private static string BuildFastImport()
    {
        var import = new StringBuilder(FileCount * 180);
        AppendBlobs(import, "before", firstMark: 1);
        AppendCommit(import, "Base files", firstBlobMark: 1, parentMark: null, commitMark: 10_001);
        AppendBlobs(import, "after", firstMark: FileCount + 1);
        AppendCommit(
            import,
            "Modify every file",
            firstBlobMark: FileCount + 1,
            parentMark: 10_001,
            commitMark: 10_002);
        import.AppendLine("done");
        return import.ToString().ReplaceLineEndings("\n");
    }

    private static void AppendBlobs(StringBuilder import, string value, int firstMark)
    {
        for (var index = 0; index < FileCount; index++)
        {
            var data = $"{value}-{index}\n";
            import.AppendLine("blob")
                .Append("mark :").AppendLine((firstMark + index).ToString())
                .Append("data ").AppendLine(Encoding.UTF8.GetByteCount(data).ToString())
                .Append(data);
        }
    }

    private static void AppendCommit(
        StringBuilder import,
        string message,
        int firstBlobMark,
        int? parentMark,
        int commitMark)
    {
        import.AppendLine("commit refs/heads/main")
            .Append("mark :").AppendLine(commitMark.ToString())
            .AppendLine("author LovelyGit Test <test@example.invalid> 1700000000 +0000")
            .AppendLine("committer LovelyGit Test <test@example.invalid> 1700000000 +0000")
            .Append("data ").AppendLine(Encoding.UTF8.GetByteCount(message).ToString())
            .AppendLine(message);
        if (parentMark is not null)
        {
            import.Append("from :").AppendLine(parentMark.Value.ToString());
        }

        for (var index = 0; index < FileCount; index++)
        {
            import.Append("M 100644 :").Append(firstBlobMark + index)
                .Append(" group-").Append((index / 100).ToString("D2"))
                .Append("/file-").Append(index.ToString("D4")).AppendLine(".txt");
        }

        import.AppendLine();
    }

    private static CliWrap.Buffered.BufferedCommandResult Run(
        DirectoryInfo directory,
        IReadOnlyList<string> arguments) =>
        new GitCliService().ExecuteBufferedAsync(arguments, directory.FullName)
            .GetAwaiter().GetResult();

    private static void DeleteDirectory(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        directory.Delete(true);
    }
}
