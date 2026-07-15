using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.FileBlame;
using LovelyGit.Tests.Git.Branches;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.FileBlame;

[Collection(PerformanceTestCollection.Name)]
public sealed class NativeFileBlamePerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task AlternatingTwentyThousandLineEdit_RemainsResponsive()
    {
        using var repository = TemporaryGitRepository.Create();
        var baseHash = await WriteCommitAsync(repository, BuildText(modified: false), "base");
        var changedHash = await WriteCommitAsync(repository, BuildText(modified: true), "alternate");
        GC.Collect();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var startedAt = Stopwatch.GetTimestamp();

        var response = await NativeFileBlameReader.ReadAsync(
            repository.Path,
            "large.txt",
            null,
            100,
            Timeout.InfiniteTimeSpan,
            CancellationToken.None);
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;

        output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}");
        output.WriteLine($"AllocatedBytes={allocated:N0}");
        Assert.Equal(20_000, response.ResolvedLineCount);
        Assert.Equal(20_000, response.Hunks.Count);
        Assert.Equal(changedHash, response.Hunks[0].Hash);
        Assert.Equal(baseHash, response.Hunks[1].Hash);
        Assert.True(elapsed < TimeSpan.FromMilliseconds(150), $"Blame took {elapsed}.");
        Assert.True(allocated < 12_000_000, $"Blame allocated {allocated:N0} bytes.");
    }

    private static string BuildText(bool modified)
    {
        var text = new StringBuilder(400_000);
        for (var index = 0; index < 20_000; index++)
        {
            text.Append(modified && (index & 1) == 0 ? "changed " : "line ")
                .Append(index).Append('\n');
        }
        return text.ToString();
    }

    private static async Task<string> WriteCommitAsync(
        TemporaryGitRepository repository,
        string content,
        string subject)
    {
        await File.WriteAllTextAsync(Path.Combine(repository.Path, "large.txt"), content);
        var git = new GitCliService();
        await git.ExecuteBufferedAsync(
            ["add", "large.txt"], repository.Path, cancellationToken: CancellationToken.None);
        await git.ExecuteBufferedAsync(
            ["commit", "-m", subject], repository.Path, cancellationToken: CancellationToken.None);
        var result = await git.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"], repository.Path, cancellationToken: CancellationToken.None);
        return result.StandardOutput.Trim();
    }
}
