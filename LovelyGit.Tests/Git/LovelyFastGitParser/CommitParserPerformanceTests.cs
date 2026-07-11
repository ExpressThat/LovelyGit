using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class CommitParserPerformanceTests
{
    private const int Iterations = 200_000;
    private readonly ITestOutputHelper _output;

    public CommitParserPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ParseUnsignedGraphCommit_HasBoundedTimeAndAllocations()
    {
        var id = GitObjectId.Parse(new string('a', 40), GitObjectFormat.Sha1);
        var data = Encoding.UTF8.GetBytes(
            $"tree {new string('b', 40)}\n" +
            $"parent {new string('c', 40)}\n" +
            "author Alice Example <alice@example.test> 1700000000 +0000\n" +
            "committer Alice Example <alice@example.test> 1700000001 +0000\n\n" +
            "Fast parser subject\n\nBody is intentionally ignored.");
        _ = GitObjectParsers.ParseCommit(id, data, includeBody: false, includeDisplayText: true);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var startedAt = Stopwatch.GetTimestamp();
        string? lastSubject = null;

        for (var index = 0; index < Iterations; index++)
        {
            var commit = GitObjectParsers.ParseCommit(
                id,
                data,
                includeBody: false,
                includeDisplayText: true);
            lastSubject = commit.Subject;
        }

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var bytesPerCommit = allocated / Iterations;
        _output.WriteLine($"{Iterations:N0} commits: {elapsed.TotalMilliseconds:N2} ms");
        _output.WriteLine($"Allocated: {bytesPerCommit:N0} bytes/commit");
        Assert.Equal("Fast parser subject", lastSubject);
        Assert.True(bytesPerCommit < 512, $"Parser allocated {bytesPerCommit} bytes per commit.");
    }
}
