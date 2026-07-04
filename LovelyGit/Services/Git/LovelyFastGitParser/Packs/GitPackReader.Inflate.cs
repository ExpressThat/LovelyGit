using System.Buffers;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

internal sealed partial class GitPackReader
{
    [ThreadStatic]
    private static Inflater? _threadInflater;

    private static byte[] InflateRemaining(
        Stream stream,
        ulong expectedSize,
        CancellationToken cancellationToken)
    {
        if (expectedSize > int.MaxValue)
        {
            throw new InvalidDataException("Packed object is too large.");
        }

        var inflater = RentInflater();
        var input = ArrayPool<byte>.Shared.Rent(8192);
        var inflated = new byte[(int)expectedSize];
        var inflatedOffset = 0;
        try
        {
            while (inflatedOffset < inflated.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (inflater.IsNeedingInput)
                {
                    var read = stream.Read(input.AsSpan(0, input.Length));
                    if (read == 0)
                    {
                        throw new EndOfStreamException("Packed object zlib stream ended unexpectedly.");
                    }

                    inflater.SetInput(input, 0, read);
                }

                var written = inflater.Inflate(
                    inflated,
                    inflatedOffset,
                    inflated.Length - inflatedOffset);
                if (written > 0)
                {
                    inflatedOffset += written;
                    continue;
                }

                if (inflater.IsNeedingDictionary)
                {
                    throw new InvalidDataException("Packed object requires an unsupported zlib dictionary.");
                }

                if (!inflater.IsNeedingInput && !inflater.IsFinished)
                {
                    throw new InvalidDataException("Packed object zlib stream made no progress.");
                }
            }

            return inflated;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(input);
        }
    }

    private static Inflater RentInflater()
    {
        var inflater = _threadInflater ??= new Inflater(noHeader: false);
        inflater.Reset();
        return inflater;
    }
}
