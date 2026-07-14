using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
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
        WriteVersion2Index(indexPath, entryCount);
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

    private static void WriteVersion2Index(string path, int entryCount)
    {
        using var stream = File.Create(path);
        Span<byte> header = stackalloc byte[12];
        "DIRC"u8.CopyTo(header);
        BinaryPrimitives.WriteUInt32BigEndian(header.Slice(4, 4), 2);
        BinaryPrimitives.WriteUInt32BigEndian(header.Slice(8, 4), (uint)entryCount);
        stream.Write(header);
        Span<byte> fixedBytes = stackalloc byte[62];
        for (var index = 0; index < entryCount; index++)
        {
            fixedBytes.Clear();
            BinaryPrimitives.WriteUInt32BigEndian(fixedBytes.Slice(24, 4), 0x81A4);
            BinaryPrimitives.WriteUInt32BigEndian(fixedBytes.Slice(36, 4), 128);
            BinaryPrimitives.WriteInt32BigEndian(fixedBytes.Slice(56, 4), index);
            var entryPath = Encoding.UTF8.GetBytes($"src/file-{index:D6}.txt");
            BinaryPrimitives.WriteUInt16BigEndian(fixedBytes.Slice(60, 2), (ushort)entryPath.Length);
            stream.Write(fixedBytes);
            stream.Write(entryPath);
            stream.WriteByte(0);
            var entryLength = fixedBytes.Length + entryPath.Length + 1;
            var padding = (8 - entryLength % 8) % 8;
            for (var count = 0; count < padding; count++) stream.WriteByte(0);
        }
    }

    private readonly record struct Measurement(TimeSpan Elapsed, long Allocated, object Result);
}
