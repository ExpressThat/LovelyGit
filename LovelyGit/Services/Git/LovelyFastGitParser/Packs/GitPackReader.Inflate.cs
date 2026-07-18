using System.IO.Compression;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

internal sealed partial class GitPackReader
{
    private static byte[] InflateRemaining(
        Stream stream,
        ulong expectedSize,
        CancellationToken cancellationToken)
    {
        if (expectedSize > int.MaxValue)
        {
            throw new InvalidDataException("Packed object is too large.");
        }

        var inflated = GC.AllocateUninitializedArray<byte>((int)expectedSize);
        var inflatedOffset = 0;
        using var zlib = new ZLibStream(stream, CompressionMode.Decompress, leaveOpen: true);
        while (inflatedOffset < inflated.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = zlib.Read(inflated.AsSpan(inflatedOffset));
            if (read == 0)
            {
                throw new EndOfStreamException("Packed object zlib stream ended unexpectedly.");
            }
            inflatedOffset += read;
        }

        return inflated;
    }
}
