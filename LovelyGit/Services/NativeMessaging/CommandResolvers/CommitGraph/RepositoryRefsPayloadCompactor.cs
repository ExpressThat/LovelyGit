using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal static class RepositoryRefsPayloadCompactor
{
    internal const int CompressionThreshold = 1_000;
    private const int SafeUncompressedCharacterBudget = 256_000;

    public static RepositoryRefsResponse CompactIfUseful(RepositoryRefsResponse response)
    {
        if (response.Refs.Count < CompressionThreshold &&
            EstimateUncompressedCharacters(response.Refs) < SafeUncompressedCharacterBudget)
        {
            return response;
        }

        var json = JsonSerializer.SerializeToUtf8Bytes(
            response.Refs,
            CommitGraphJsonSerializerContext.Default.ListRepositoryRefItem);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(json);
        }

        return response with
        {
            Refs = [],
            CompactRefsGzipBase64 = Convert.ToBase64String(output.GetBuffer(), 0, checked((int)output.Length)),
        };
    }

    private static long EstimateUncompressedCharacters(IEnumerable<RepositoryRefItem> refs)
    {
        long characters = 0;
        foreach (var reference in refs)
        {
            characters += 64L + reference.Name.Length + reference.CommitHash.Length +
                          (reference.RemoteUrl?.Length ?? 0);
            if (characters >= SafeUncompressedCharacterBudget) break;
        }

        return characters;
    }
}
