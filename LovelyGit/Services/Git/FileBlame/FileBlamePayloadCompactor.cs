using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

namespace ExpressThat.LovelyGit.Services.Git.FileBlame;

internal static class FileBlamePayloadCompactor
{
    internal const int CompressionThreshold = 256_000;
    internal const int MaximumCompactCharacters = 800_000;

    public static FileBlameResponse CompactIfUseful(FileBlameResponse response)
    {
        if (response.Content.Length < CompressionThreshold && response.Hunks.Count < 1_000)
        {
            return response;
        }

        var json = JsonSerializer.SerializeToUtf8Bytes(
            response,
            CommitGraphJsonSerializerContext.Default.FileBlameResponse);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(json);
        }

        var compact = Convert.ToBase64String(
            output.GetBuffer(), 0, checked((int)output.Length));
        if (compact.Length > MaximumCompactCharacters)
        {
            throw new InvalidDataException(
                "File blame content could not be compacted enough for the desktop viewer.");
        }

        return response with
        {
            CompactPayloadGzipBase64 = compact,
            Content = string.Empty,
            Hunks = [],
        };
    }
}
