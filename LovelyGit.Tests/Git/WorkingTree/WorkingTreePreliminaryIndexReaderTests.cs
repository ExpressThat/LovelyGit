using System.Buffers.Binary;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreePreliminaryIndexReaderTests
{
    [Theory]
    [InlineData(2u)]
    [InlineData(3u)]
    public void CountMissingRootEntries_ReadsPaddedIndexVersions(uint version)
    {
        using var directory = TemporaryDirectory.Create("lovelygit-preliminary-index-");
        var indexPath = Path.Combine(directory.Path, "index");
        WriteIndex(indexPath, version, ["src/first.cs", "src/second.cs"]);

        var missing = WorkingTreePreliminaryIndexReader.CountMissingRootEntries(
            indexPath,
            ["docs", "src"],
            CancellationToken.None);

        Assert.Equal(1, missing);
    }

    [Fact]
    public async Task CountMissingRootEntries_ReadsGitProducedVersionFourIndex()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-preliminary-index-v4-");
        await GitTestProcess.RunAsync(directory.Path, "init");
        Directory.CreateDirectory(Path.Combine(directory.Path, "src"));
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "src", "first.cs"), "first");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "src", "second.cs"), "second");
        await GitTestProcess.RunAsync(directory.Path, "add", ".");
        await GitTestProcess.RunAsync(directory.Path, "update-index", "--index-version", "4");

        var missing = WorkingTreePreliminaryIndexReader.CountMissingRootEntries(
            Path.Combine(directory.Path, ".git", "index"),
            ["docs", "src"],
            CancellationToken.None);

        Assert.Equal(1, missing);
    }

    [Fact]
    public void CountMissingRootEntries_ThrowsForTruncatedEntry()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-preliminary-index-truncated-");
        var indexPath = Path.Combine(directory.Path, "index");
        WriteHeader(indexPath, version: 2, entries: 1);

        Assert.Throws<EndOfStreamException>(() =>
            WorkingTreePreliminaryIndexReader.CountMissingRootEntries(
                indexPath,
                ["src"],
                CancellationToken.None));
    }

    [Fact]
    public void CountMissingRootEntries_HonorsCancellationWithoutReturningPartialState()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-preliminary-index-cancel-");
        var indexPath = Path.Combine(directory.Path, "index");
        WriteIndex(indexPath, 2, Enumerable.Range(0, 100).Select(index => $"src/{index:D4}.cs"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            WorkingTreePreliminaryIndexReader.CountMissingRootEntries(
                indexPath,
                ["src"],
                cancellation.Token));
    }

    [Fact]
    public void CountMissingRootEntries_DoesNotAllocatePerTrackedEntry()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-preliminary-index-allocation-");
        var indexPath = Path.Combine(directory.Path, "index");
        WriteIndex(indexPath, 2, Enumerable.Range(0, 10_000).Select(index => $"src/{index:D5}.cs"));
        _ = WorkingTreePreliminaryIndexReader.CountMissingRootEntries(
            indexPath,
            ["src"],
            CancellationToken.None);
        var before = GC.GetAllocatedBytesForCurrentThread();

        _ = WorkingTreePreliminaryIndexReader.CountMissingRootEntries(
            indexPath,
            ["src"],
            CancellationToken.None);

        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 16 * 1024);
    }

    private static void WriteIndex(string path, uint version, IEnumerable<string> paths)
    {
        var entries = paths.ToArray();
        using var stream = File.Create(path);
        Span<byte> header = stackalloc byte[12];
        "DIRC"u8.CopyTo(header);
        BinaryPrimitives.WriteUInt32BigEndian(header[4..], version);
        BinaryPrimitives.WriteUInt32BigEndian(header[8..], (uint)entries.Length);
        stream.Write(header);
        Span<byte> fixedBytes = stackalloc byte[62];
        foreach (var entryPath in entries)
        {
            var entryStart = stream.Position;
            var pathBytes = Encoding.UTF8.GetBytes(entryPath);
            fixedBytes.Clear();
            var flags = (ushort)pathBytes.Length;
            if (version == 3)
            {
                flags |= 0x4000;
            }

            BinaryPrimitives.WriteUInt16BigEndian(fixedBytes[60..], flags);
            stream.Write(fixedBytes);
            if (version == 3)
            {
                stream.Write([0, 0]);
            }

            stream.Write(pathBytes);
            stream.WriteByte(0);
            while ((stream.Position - entryStart) % 8 != 0)
            {
                stream.WriteByte(0);
            }
        }
    }

    private static void WriteHeader(string path, uint version, uint entries)
    {
        Span<byte> header = stackalloc byte[12];
        "DIRC"u8.CopyTo(header);
        BinaryPrimitives.WriteUInt32BigEndian(header[4..], version);
        BinaryPrimitives.WriteUInt32BigEndian(header[8..], entries);
        File.WriteAllBytes(path, header.ToArray());
    }
}
