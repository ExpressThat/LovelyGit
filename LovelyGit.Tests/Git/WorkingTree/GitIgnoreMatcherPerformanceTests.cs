using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.WorkingTree;

[Collection(PerformanceTestCollection.Name)]
public sealed class GitIgnoreMatcherPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task LoadAsync_StreamsLargeConfigAndIgnoreSources()
    {
        using var root = TemporaryDirectory.Create("lovelygit-ignore-performance-");
        var gitDirectory = Directory.CreateDirectory(Path.Combine(root.Path, ".git"));
        Directory.CreateDirectory(Path.Combine(gitDirectory.FullName, "info"));
        await File.WriteAllTextAsync(
            Path.Combine(gitDirectory.FullName, "config"), BuildConfig());
        await File.WriteAllTextAsync(
            Path.Combine(root.Path, ".gitignore"), BuildIgnoreFile());
        GC.Collect();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var startedAt = Stopwatch.GetTimestamp();

        var matcher = await GitIgnoreMatcher.LoadAsync(
            root.Path, gitDirectory.FullName, CancellationToken.None);

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        startedAt = Stopwatch.GetTimestamp();
        Assert.True(matcher.IsIgnored("generated/file-09999.tmp", isDirectory: false));
        Assert.False(matcher.IsIgnored("source/file.cs", isDirectory: false));
        var lookupElapsed = Stopwatch.GetElapsedTime(startedAt);
        output.WriteLine(
            $"LoadMs={elapsed.TotalMilliseconds:F2}; LookupMs={lookupElapsed.TotalMilliseconds:F2}; " +
            $"AllocatedBytes={allocated:N0}");
        Assert.True(elapsed < TimeSpan.FromMilliseconds(100), $"Ignore load took {elapsed}.");
        Assert.True(lookupElapsed < TimeSpan.FromMilliseconds(100), $"Ignore lookup took {lookupElapsed}.");
        Assert.True(allocated < 5_000_000, $"Ignore load allocated {allocated:N0} bytes.");
    }

    private static string BuildConfig()
    {
        var config = new StringBuilder(1_000_000).AppendLine("[core]")
            .AppendLine("\tbare = false");
        for (var index = 0; index < 10_000; index++)
        {
            config.Append("[branch \"perf-").Append(index.ToString("D5"))
                .AppendLine("\"]").AppendLine("\tremote = origin")
                .Append("\tmerge = refs/heads/perf-").AppendLine(index.ToString("D5"));
        }
        return config.ToString();
    }

    private static string BuildIgnoreFile()
    {
        var ignore = new StringBuilder(4_000_000);
        for (var index = 0; index < 100_000; index++)
            ignore.Append("# explanatory comment ").AppendLine(index.ToString("D6"));
        for (var index = 0; index < 10_000; index++)
            ignore.Append("generated/file-").Append(index.ToString("D5")).AppendLine(".tmp");
        return ignore.ToString();
    }
}
