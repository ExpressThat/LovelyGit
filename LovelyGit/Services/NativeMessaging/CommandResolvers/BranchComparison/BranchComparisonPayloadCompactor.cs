using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.BranchComparison;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.BranchComparison;

internal static class BranchComparisonPayloadCompactor
{
    internal const int CompressionThreshold = 500;
    private const int SafeUncompressedCharacterBudget = 128_000;

    public static BranchComparisonResponse CompactIfUseful(BranchComparisonResponse response)
    {
        if (response.Files.Count < CompressionThreshold &&
            EstimateUncompressedCharacters(response.Files) < SafeUncompressedCharacterBudget)
        {
            return response;
        }

        var json = JsonSerializer.SerializeToUtf8Bytes(
            response.Files,
            BranchComparisonJsonSerializerContext.Default.ListBranchComparisonFile);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(json);
        }

        return response with
        {
            Files = [],
            CompactFilesGzipBase64 = Convert.ToBase64String(
                output.GetBuffer(), 0, checked((int)output.Length)),
        };
    }

    private static long EstimateUncompressedCharacters(IEnumerable<BranchComparisonFile> files)
    {
        long characters = 0;
        foreach (var file in files)
        {
            characters += 32L + file.Path.Length + file.Status.Length;
            if (characters >= SafeUncompressedCharacterBudget) break;
        }

        return characters;
    }
}
