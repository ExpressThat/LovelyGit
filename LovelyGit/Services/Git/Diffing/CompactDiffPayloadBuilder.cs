using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using System.IO.Compression;
using System.Text.Json;

namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static class CompactDiffPayloadBuilder
{
    private const int MinimumRows = 5_000;
    private const int MinimumCharacters = 256_000;

    public static CommitFileDiffResponse CompactIfUseful(CommitFileDiffResponse response)
    {
        if (response.Lines.Count < MinimumRows || HasVirtualPayload(response))
        {
            return response;
        }

        if (EstimateTextCharacters(response.Lines) < MinimumCharacters)
        {
            return response;
        }

        response.CompactLineSchema = "tuple-v1:gzip-base64:utf-8";
        response.CompactLinesGzipBase64 = CompressLines(response.Lines);
        response.CompactLineCount = response.Lines.Count;
        response.Lines = [];
        return response;
    }

    private static bool HasVirtualPayload(CommitFileDiffResponse response) =>
        !string.IsNullOrEmpty(response.VirtualText)
        || !string.IsNullOrEmpty(response.VirtualTextGzipBase64);

    private static int EstimateTextCharacters(IReadOnlyList<CommitFileDiffLine> lines)
    {
        var count = 0;
        foreach (var line in lines)
        {
            count += line.OldText.Length + line.NewText.Length + line.Text.Length;
        }

        return count;
    }

    private static string CompressLines(IReadOnlyList<CommitFileDiffLine> lines)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            using var writer = new Utf8JsonWriter(gzip);
            writer.WriteStartArray();
            foreach (var line in lines)
            {
                WriteLine(writer, line);
            }

            writer.WriteEndArray();
        }

        return Convert.ToBase64String(output.GetBuffer(), 0, (int)output.Length);
    }

    private static void WriteLine(Utf8JsonWriter writer, CommitFileDiffLine line)
    {
        writer.WriteStartArray();
        WriteNullableNumber(writer, line.OldLineNumber);
        WriteNullableNumber(writer, line.NewLineNumber);
        writer.WriteStringValue(line.OldText);
        writer.WriteStringValue(line.NewText);
        writer.WriteStringValue(line.Text);
        writer.WriteStringValue(line.ChangeType);
        writer.WriteEndArray();
    }

    private static void WriteNullableNumber(Utf8JsonWriter writer, int? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
