using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class ConflictTextPayloadBuilder
{
    private const int MinimumCharacters = 64_000;
    private const int MaximumRetainedSourceCharacters = 2 * 1024 * 1024;
    private const string BundleSchema = "interleaved-lines-v3:gzip-base64:varint-utf-8";
    private const string LegacyBundleSchema = "interleaved-lines-v2:gzip-base64:utf-8";

    public static void Compact(ConflictResolutionResponse response)
    {
        var versions = new[] { response.Base, response.Ours, response.Theirs, response.Result };
        if (versions.Sum(version => version.Text?.Length ?? 0) < MinimumCharacters)
        {
            return;
        }

        response.CompactTextSchema = BundleSchema;
        response.CompactTextBundleGzipBase64 = ConflictTextBundleCodec.Compress(
            response.Base.Text,
            response.Ours.Text,
            response.Theirs.Text,
            response.Result.Text);
        foreach (var version in versions)
        {
            version.Text = null;
            version.TextGzipBase64 = null;
            version.TextEncoding = null;
        }
    }

    public static ConflictTexts? RetainSources(ConflictResolutionResponse response)
    {
        var characterCount = response.Base.Text?.Length ?? 0;
        characterCount += response.Ours.Text?.Length ?? 0;
        characterCount += response.Theirs.Text?.Length ?? 0;
        if (characterCount < MinimumCharacters || characterCount > MaximumRetainedSourceCharacters)
        {
            return null;
        }

        return new(response.Base.Text, response.Ours.Text, response.Theirs.Text, null);
    }

    public static ConflictTexts Expand(ConflictResolutionResponse response)
    {
        if (response.CompactTextBundleGzipBase64 is not { } bundle)
        {
            return new(
                response.Base.Text,
                response.Ours.Text,
                response.Theirs.Text,
                response.Result.Text);
        }

        if (response.CompactTextSchema == BundleSchema)
        {
            return ConflictTextBundleCodec.Expand(bundle);
        }
        if (response.CompactTextSchema != LegacyBundleSchema)
        {
            throw new InvalidOperationException($"Unsupported conflict text schema: {response.CompactTextSchema}");
        }

        var bytes = Convert.FromBase64String(bundle);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var document = JsonDocument.Parse(gzip);
        var sources = new[] { new StringBuilder(), new StringBuilder(), new StringBuilder() };
        foreach (var row in document.RootElement[0].EnumerateArray())
        {
            for (var index = 0; index < sources.Length; index++)
            {
                if (row[index].ValueKind is not JsonValueKind.Null)
                {
                    sources[index].Append(row[index].GetString());
                }
            }
        }

        return new(
            sources[0].ToString(),
            sources[1].ToString(),
            sources[2].ToString(),
            document.RootElement[1].GetString());
    }

}

internal readonly record struct ConflictTexts(string? Base, string? Ours, string? Theirs, string? Result);
