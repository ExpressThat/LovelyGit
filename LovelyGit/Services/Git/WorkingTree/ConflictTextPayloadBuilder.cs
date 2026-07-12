using System.IO.Compression;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class ConflictTextPayloadBuilder
{
    private const int MinimumCharacters = 64_000;
    private const string EncodingName = "gzip-base64:utf-8";

    public static void Compact(ConflictResolutionResponse response)
    {
        Compact(response.Base);
        Compact(response.Ours);
        Compact(response.Theirs);
        Compact(response.Result);
    }

    private static void Compact(ConflictFileVersion version)
    {
        if (version.Text is not { Length: >= MinimumCharacters } text)
        {
            return;
        }

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            gzip.Write(bytes);
        }

        version.TextGzipBase64 = Convert.ToBase64String(
            output.GetBuffer(),
            0,
            checked((int)output.Length));
        version.TextEncoding = EncodingName;
        version.Text = null;
    }
}
