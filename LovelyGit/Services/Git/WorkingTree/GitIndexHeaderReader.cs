using System.Buffers.Binary;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class GitIndexHeaderReader
{
    public static async Task<uint?> ReadEntryCountAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var indexPath = Path.Combine(gitDirectory, "index");
        if (!File.Exists(indexPath))
        {
            return 0;
        }

        await using var stream = new FileStream(indexPath, new FileStreamOptions
        {
            Access = FileAccess.Read,
            BufferSize = 12,
            Mode = FileMode.Open,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            Share = FileShare.ReadWrite | FileShare.Delete,
        });
        var header = new byte[12];
        try
        {
            await stream.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
        }
        catch (EndOfStreamException)
        {
            return null;
        }

        var span = header.AsSpan();
        if (!span[..4].SequenceEqual("DIRC"u8))
        {
            return null;
        }

        var version = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(4, 4));
        return version is 2 or 3 or 4
            ? BinaryPrimitives.ReadUInt32BigEndian(span.Slice(8, 4))
            : null;
    }
}
