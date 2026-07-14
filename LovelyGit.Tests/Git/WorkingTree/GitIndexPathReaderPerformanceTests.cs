using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.WorkingTree;

[Collection(PerformanceTestCollection.Name)]
public sealed class GitIndexPathReaderPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task TargetedRead_AvoidsMaterializingAChromiumScaleIndex()
    {
        const int entryCount = 100_000;
        using var directory = TemporaryDirectory.Create("lovelygit-large-index-");
        var gitDirectory = Directory.CreateDirectory(Path.Combine(directory.Path, ".git")).FullName;
        var indexPath = Path.Combine(gitDirectory, "index");
        SyntheticGitIndexWriter.WriteVersion2(indexPath, entryCount);
        var reader = new GitIndexReader();
        var target = $"src/file-{entryCount - 1:D6}.txt";
        _ = await reader.ReadEntriesForPathAsync(
            gitDirectory, GitObjectFormat.Sha1, target, CancellationToken.None);

        var targeted = Measure(() => reader.ReadEntriesForPathAsync(
            gitDirectory, GitObjectFormat.Sha1, target, CancellationToken.None).GetAwaiter().GetResult());
        var full = Measure(() => reader.ReadAsync(
            gitDirectory, GitObjectFormat.Sha1, CancellationToken.None).GetAwaiter().GetResult());

        output.WriteLine($"Targeted: {targeted.Elapsed.TotalMilliseconds:N1} ms, {targeted.Allocated:N0} bytes");
        output.WriteLine($"Full: {full.Elapsed.TotalMilliseconds:N1} ms, {full.Allocated:N0} bytes");
        Assert.Equal(target, Assert.Single((IReadOnlyList<GitIndexEntry>)targeted.Result).Path);
        Assert.True(
            targeted.Allocated < full.Allocated * 0.05,
            $"Targeted read allocated {targeted.Allocated:N0} vs full read {full.Allocated:N0} bytes.");
    }

    private static Measurement Measure(Func<object> action)
    {
        GC.Collect();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var startedAt = Stopwatch.GetTimestamp();
        var result = action();
        return new(
            Stopwatch.GetElapsedTime(startedAt),
            GC.GetAllocatedBytesForCurrentThread() - allocatedBefore,
            result);
    }

    private readonly record struct Measurement(TimeSpan Elapsed, long Allocated, object Result);
}
