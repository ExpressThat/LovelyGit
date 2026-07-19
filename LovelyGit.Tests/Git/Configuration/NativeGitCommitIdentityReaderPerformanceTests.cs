using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.Configuration;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.Configuration;

[Collection(GitIdentityPerformanceCollection.Name)]
public sealed class NativeGitCommitIdentityReaderPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public NativeGitCommitIdentityReaderPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ReadAsync_RemainsBoundedWithTenThousandUnrelatedRemotes()
    {
        using var repository = TestIdentityRepository.Create();
        await repository.AppendLocalConfigAsync(BuildRemoteConfiguration(10_000));
        await repository.AppendLocalConfigAsync(
            "[user]\n\tname = Performance User\n\temail = performance@example.test\n");
        var reader = new NativeGitCommitIdentityReader();
        await reader.ReadAsync(repository.Path, repository.CreateOptions(), CancellationToken.None);

        const int iterations = 5;
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var startedAt = Stopwatch.GetTimestamp();
        GitCommitIdentity? identity = null;
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            identity = await reader.ReadAsync(
                repository.Path,
                repository.CreateOptions(),
                CancellationToken.None);
        }

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        _output.WriteLine(
            "Five reads: {0:F2} ms, {1:N0} allocated bytes",
            elapsed.TotalMilliseconds,
            allocated);
        Assert.Equal("Performance User", identity?.Name);
        Assert.Equal("performance@example.test", identity?.Email);
        Assert.True(elapsed < TimeSpan.FromMilliseconds(250));
        Assert.InRange(allocated, 0, 2_000_000);
    }

    private static string BuildRemoteConfiguration(int count)
    {
        var builder = new StringBuilder(count * 96);
        for (var index = 0; index < count; index++)
        {
            builder.Append("[remote \"remote-")
                .Append(index.ToString("D5"))
                .Append("\"]\n\turl = https://example.test/")
                .Append(index)
                .Append("/repository.git\n\tfetch = +refs/heads/*:refs/remotes/remote-")
                .Append(index.ToString("D5"))
                .Append("/*\n");
        }

        return builder.ToString();
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GitIdentityPerformanceCollection
{
    public const string Name = "Git identity performance";
}
