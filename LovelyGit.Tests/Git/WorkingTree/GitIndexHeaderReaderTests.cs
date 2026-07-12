using System.Buffers.Binary;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class GitIndexHeaderReaderTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task ReadEntryCountAsync_ReadsSupportedIndexHeader(uint version)
    {
        using var directory = TemporaryDirectory.Create("lovelygit-index-header-");
        var header = CreateHeader(version, 45_678);
        await File.WriteAllBytesAsync(Path.Combine(directory.Path, "index"), header);

        var count = await GitIndexHeaderReader.ReadEntryCountAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Equal(45_678u, count);
    }

    [Fact]
    public async Task ReadEntryCountAsync_MissingIndexReturnsZero()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-index-header-");

        var count = await GitIndexHeaderReader.ReadEntryCountAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Equal(0u, count);
    }

    [Theory]
    [InlineData("invalid signature", 2)]
    [InlineData("unsupported version", 5)]
    [InlineData("truncated header", 2)]
    public async Task ReadEntryCountAsync_InvalidHeaderDefersToFullParser(
        string scenario,
        uint version)
    {
        using var directory = TemporaryDirectory.Create("lovelygit-index-header-");
        var header = CreateHeader(version, 10);
        if (scenario == "invalid signature") header[0] = (byte)'X';
        if (scenario == "truncated header") Array.Resize(ref header, 8);
        await File.WriteAllBytesAsync(Path.Combine(directory.Path, "index"), header);

        var count = await GitIndexHeaderReader.ReadEntryCountAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Null(count);
    }

    private static byte[] CreateHeader(uint version, uint count)
    {
        var header = new byte[12];
        "DIRC"u8.CopyTo(header);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), version);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8), count);
        return header;
    }
}
