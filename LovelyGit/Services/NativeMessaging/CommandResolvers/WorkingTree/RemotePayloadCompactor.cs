using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal static class RemotePayloadCompactor
{
    internal const int CompressionThreshold = 250;
    private const int SafeUncompressedCharacterBudget = 128_000;

    internal static GetRemotesResponse CompactIfUseful(List<GitRemote> remotes)
    {
        if (remotes.Count < CompressionThreshold &&
            EstimateUncompressedCharacters(remotes) < SafeUncompressedCharacterBudget)
        {
            return new GetRemotesResponse { Remotes = remotes };
        }

        var json = JsonSerializer.SerializeToUtf8Bytes(
            remotes,
            WorkingTreeJsonSerializerContext.Default.ListGitRemote);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(json);
        }

        return new GetRemotesResponse
        {
            CompactRemotesGzipBase64 = Convert.ToBase64String(
                output.GetBuffer(), 0, checked((int)output.Length)),
        };
    }

    private static long EstimateUncompressedCharacters(IEnumerable<GitRemote> remotes)
    {
        long characters = 0;
        foreach (var remote in remotes)
        {
            characters += 32L + remote.Name.Length + remote.Url.Length +
                (remote.PushUrl?.Length ?? 0);
            if (characters >= SafeUncompressedCharacterBudget) break;
        }

        return characters;
    }
}
