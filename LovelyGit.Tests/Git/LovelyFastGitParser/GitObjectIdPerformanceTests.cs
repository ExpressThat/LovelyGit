using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitObjectIdPerformanceTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 32)]
    public void FromBytes_ProducesTheExpectedIdentity(int formatValue, int byteLength)
    {
        var format = (GitObjectFormat)formatValue;
        var bytes = Enumerable.Range(0, byteLength).Select(index => (byte)index).ToArray();

        var id = GitObjectId.FromBytes(bytes, format);

        Assert.Equal(Convert.ToHexString(bytes).ToLowerInvariant(), id.Value);
        Assert.Equal(format, id.ObjectFormat);
    }

    [Fact]
    public void FromBytes_DoesNotAllocateAnIntermediateCharacterBuffer()
    {
        Span<byte> bytes = stackalloc byte[20];
        bytes.Fill(0xab);
        _ = GitObjectId.FromBytes(bytes, GitObjectFormat.Sha1);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var length = 0;

        for (var index = 0; index < 10_000; index++)
        {
            length += GitObjectId.FromBytes(bytes, GitObjectFormat.Sha1).Value.Length;
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        output.WriteLine($"AllocatedBytes={allocated:N0}");
        Assert.Equal(400_000, length);
        Assert.True(allocated < 1_200_000, $"Conversions allocated {allocated:N0} bytes.");
    }
}
