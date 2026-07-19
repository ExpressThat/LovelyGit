using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using LovelyGit.Tests.Git.Branches;
using LovelyGit.Tests.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

[Collection(PerformanceTestCollection.Name)]
public sealed class PackedObjectReadPerformanceTests
{
    private const int Iterations = 10_000;
    private static readonly RepositoryTemplate<GitObjectId> Template = new(
        "lovelygit-packed-read-template-",
        CreateTemplate,
        prewarmCopies: 1);
    private readonly ITestOutputHelper _output;

    public PackedObjectReadPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task RepeatedCommitReads_AvoidPerObjectMetadataQueriesAndLargeInflaterState()
    {
        var (directory, commitId) = Template.CreateCopy("lovelygit-packed-read-");
        try
        {
            using var store = new GitObjectStore(
                Path.Combine(directory.FullName, ".git"),
                GitObjectFormat.Sha1);
            _ = await store.ReadObjectWithoutCachingAsync(commitId, CancellationToken.None);
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();
            GitObjectData? result = null;

            for (var index = 0; index < Iterations; index++)
            {
                result = await store.ReadObjectWithoutCachingAsync(
                    commitId,
                    CancellationToken.None);
            }

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
            var bytesPerRead = allocated / Iterations;
            _output.WriteLine(
                $"{Iterations:N0} reads: {elapsed.TotalMilliseconds:N2} ms, " +
                $"{bytesPerRead:N0} bytes/read");
            Assert.Equal(GitObjectKind.Commit, result?.Kind);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(450),
                $"Packed reads took {elapsed.TotalMilliseconds:N2} ms.");
            Assert.True(bytesPerRead < 4_000,
                $"Packed reads allocated {bytesPerRead:N0} bytes/read.");
        }
        finally
        {
            TemporaryGitDirectory.Delete(directory);
        }
    }

    private static GitObjectId CreateTemplate(DirectoryInfo directory)
    {
        var head = InitializedRepositoryTemplate.CopyInto(directory);
        var git = new GitCliService();
        git.ExecuteBufferedAsync(
            ["gc", "--prune=now"], directory.FullName).GetAwaiter().GetResult();
        return GitObjectId.Parse(head);
    }
}
