using System.Security.Cryptography;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictWorktreeSnapshotReaderTests
{
    [Fact]
    public async Task ReadAsync_ReturnsExactBytesFingerprintAndStableStamp()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-conflict-snapshot-");
        var path = Path.Combine(directory.Path, "conflict.txt");
        var expected = "current\r\n<<<<<<< conflict\nresult"u8.ToArray();
        await File.WriteAllBytesAsync(path, expected);

        var snapshot = await ConflictWorktreeSnapshotReader.ReadAsync(path, 1024, CancellationToken.None);

        Assert.Equal(expected, snapshot.Bytes);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(expected)), snapshot.Fingerprint);
        Assert.Equal(expected.Length, snapshot.Stamp.Length);
        Assert.True(snapshot.Stamp.Exists);
        Assert.False(snapshot.IsTooLarge);
    }

    [Fact]
    public async Task ReadAsync_HashesLargeFileWithoutRetainingItsBytes()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-conflict-snapshot-");
        var path = Path.Combine(directory.Path, "large.txt");
        var expected = Enumerable.Range(0, 256).Select(index => (byte)index).ToArray();
        await File.WriteAllBytesAsync(path, expected);

        var snapshot = await ConflictWorktreeSnapshotReader.ReadAsync(path, 32, CancellationToken.None);

        Assert.Null(snapshot.Bytes);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(expected)), snapshot.Fingerprint);
        Assert.Equal(expected.Length, snapshot.Stamp.Length);
        Assert.True(snapshot.IsTooLarge);
    }

    [Fact]
    public async Task ReadAsync_ReturnsMissingSnapshotWithoutOpeningAFile()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-conflict-snapshot-");

        var snapshot = await ConflictWorktreeSnapshotReader.ReadAsync(
            Path.Combine(directory.Path, "missing.txt"),
            1024,
            CancellationToken.None);

        Assert.Equal("missing", snapshot.Fingerprint);
        Assert.False(snapshot.Stamp.Exists);
        Assert.Null(snapshot.Bytes);
        Assert.False(snapshot.IsTooLarge);
    }

    [Fact]
    public async Task ReadAsync_HonorsPreCancelledRequestWithoutReadingFile()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-conflict-snapshot-");
        var path = Path.Combine(directory.Path, "conflict.txt");
        await File.WriteAllTextAsync(path, "content");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ConflictWorktreeSnapshotReader.ReadAsync(path, 1024, cancellation.Token));
    }
}
