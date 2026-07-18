using System.IO.Compression;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class CommitPatchPayloadCompactor
{
    internal const int CompressionThreshold = 256_000;
    internal const int MaximumCompactCharacters = 800_000;

    public static CommitPatchResponse Compact(CommitPatchResponse response)
    {
        if (response.IsTruncated) return response with { Patch = string.Empty };
        var compact = Compress(response.Patch);
        if (compact is null) return response;
        return compact.Length <= MaximumCompactCharacters
            ? response with { Patch = string.Empty, CompactPatchGzipBase64 = compact }
            : response with { Patch = string.Empty, IsTruncated = true };
    }

    public static CommitPatchSeriesResponse Compact(CommitPatchSeriesResponse response)
    {
        if (response.IsTruncated) return response with { Patch = string.Empty };
        var compact = Compress(response.Patch);
        if (compact is null) return response;
        return compact.Length <= MaximumCompactCharacters
            ? response with { Patch = string.Empty, CompactPatchGzipBase64 = compact }
            : response with { Patch = string.Empty, IsTruncated = true };
    }

    private static string? Compress(string patch)
    {
        if (patch.Length < CompressionThreshold) return null;
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        using (var writer = new StreamWriter(
                   gzip, new UTF8Encoding(false), bufferSize: 16_384, leaveOpen: true))
        {
            writer.Write(patch.AsSpan());
        }
        return Convert.ToBase64String(output.GetBuffer(), 0, checked((int)output.Length));
    }
}
