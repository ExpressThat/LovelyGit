using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Data.Models;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

internal static class KnownRepositoriesPayloadCompactor
{
    internal const int CompressionThreshold = 512;

    public static KnownGitRepositoriesResponse Compact(
        List<KnownGitRepository> repositories)
    {
        if (repositories.Count < CompressionThreshold)
        {
            return new KnownGitRepositoriesResponse { Repositories = repositories };
        }

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(
            output,
            CompressionLevel.Fastest,
            leaveOpen: true))
        {
            JsonSerializer.Serialize(
                gzip,
                repositories,
                KnownRepositoriesJsonSerializerContext.Default.ListKnownGitRepository);
        }

        return new KnownGitRepositoriesResponse
        {
            CompactRepositoriesGzipBase64 = Convert.ToBase64String(
                output.GetBuffer().AsSpan(0, checked((int)output.Length))),
        };
    }
}
