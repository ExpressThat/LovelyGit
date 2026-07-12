using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class ConflictTextPayloadBuilder
{
    private const int MinimumCharacters = 64_000;
    private const string BundleSchema = "interleaved-lines-v2:gzip-base64:utf-8";

    public static void Compact(ConflictResolutionResponse response)
    {
        var versions = new[] { response.Base, response.Ours, response.Theirs, response.Result };
        if (versions.Sum(version => version.Text?.Length ?? 0) < MinimumCharacters)
        {
            return;
        }

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        using (var writer = new Utf8JsonWriter(gzip))
        {
            writer.WriteStartArray();
            WriteInterleavedSources(writer, response.Base.Text, response.Ours.Text, response.Theirs.Text);
            writer.WriteStringValue(response.Result.Text);
            writer.WriteEndArray();
        }

        response.CompactTextSchema = BundleSchema;
        response.CompactTextBundleGzipBase64 = Convert.ToBase64String(
            output.GetBuffer(),
            0,
            checked((int)output.Length));
        foreach (var version in versions)
        {
            version.Text = null;
            version.TextGzipBase64 = null;
            version.TextEncoding = null;
        }
    }

    private static void WriteInterleavedSources(
        Utf8JsonWriter writer,
        string? baseText,
        string? oursText,
        string? theirsText)
    {
        var texts = new[] { baseText, oursText, theirsText };
        var positions = new int[3];
        writer.WriteStartArray();
        while (HasRemainingText(texts, positions))
        {
            writer.WriteStartArray();
            for (var index = 0; index < texts.Length; index++)
            {
                WriteNextLine(writer, texts[index], ref positions[index]);
            }
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }

    private static bool HasRemainingText(string?[] texts, int[] positions)
    {
        for (var index = 0; index < texts.Length; index++)
        {
            if (texts[index] is { } text && positions[index] < text.Length) return true;
        }
        return false;
    }

    private static void WriteNextLine(Utf8JsonWriter writer, string? text, ref int position)
    {
        if (text is null || position >= text.Length)
        {
            writer.WriteNullValue();
            return;
        }

        var newline = text.AsSpan(position).IndexOf('\n');
        var length = newline < 0 ? text.Length - position : newline + 1;
        writer.WriteStringValue(text.AsSpan(position, length));
        position += length;
    }
}
