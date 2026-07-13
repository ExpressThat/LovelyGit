using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static class ReferencedDiffPayloadBuilder
{
    public const string LineSchema = "tuple-v4-delta-refs:gzip-base64:utf-8";
    public const string SourceSchema = "interleaved-lines-v3:gzip-base64:varint-utf-8";

    public static CommitFileDiffResponse Build(
        string commitHash,
        string path,
        string status,
        CommitDiffViewMode viewMode,
        LineDiffModel model,
        string? oldText = null,
        string? newText = null)
    {
        var compact = Compress(model, viewMode);
        var response = new CommitFileDiffResponse
        {
            CommitHash = commitHash,
            Path = path,
            Status = status,
            ViewMode = viewMode,
            HasDifferences = model.HasDifferences,
            CompactLineSchema = LineSchema,
            CompactLinesGzipBase64 = compact.Payload,
            CompactLineCount = compact.LineCount,
        };
        if (oldText is not null && newText is not null)
        {
            response.CompactSourceSchema = SourceSchema;
            response.CompactSourceBundleGzipBase64 = ConflictTextBundleCodec.Compress(
                oldText,
                newText,
                null,
                null,
                CompressionLevel.Optimal);
        }
        return response;
    }

    private static CompactPayload Compress(LineDiffModel model, CommitDiffViewMode viewMode)
    {
        using var output = new MemoryStream();
        var lineCount = 0;
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        using (var writer = new Utf8JsonWriter(gzip))
        {
            writer.WriteStartArray();
            var previousOld = 0;
            var previousNew = 0;
            foreach (var row in model.Rows)
            {
                if (viewMode == CommitDiffViewMode.Combined
                    && row.IsChanged
                    && row.OldIndex is { } oldIndex
                    && row.NewIndex is { } newIndex)
                {
                    WriteReferenceLine(writer, oldIndex, null, "Deleted", ref previousOld, ref previousNew);
                    WriteReferenceLine(writer, null, newIndex, "Inserted", ref previousOld, ref previousNew);
                    lineCount += 2;
                }
                else
                {
                    WriteLine(writer, model, row, ref previousOld, ref previousNew);
                    lineCount++;
                }
            }
            writer.WriteEndArray();
        }
        return new(Convert.ToBase64String(output.GetBuffer(), 0, checked((int)output.Length)), lineCount);
    }

    private static void WriteReferenceLine(
        Utf8JsonWriter writer,
        int? oldIndex,
        int? newIndex,
        string changeType,
        ref int previousOld,
        ref int previousNew)
    {
        writer.WriteStartArray();
        WriteDelta(writer, oldIndex + 1, ref previousOld);
        WriteDelta(writer, newIndex + 1, ref previousNew);
        WriteChangeType(writer, changeType);
        writer.WriteEndArray();
    }

    private static void WriteLine(
        Utf8JsonWriter writer,
        LineDiffModel model,
        LineDiffRow row,
        ref int previousOld,
        ref int previousNew)
    {
        writer.WriteStartArray();
        WriteDelta(writer, row.OldIndex + 1, ref previousOld);
        WriteDelta(writer, row.NewIndex + 1, ref previousNew);
        WriteChangeType(writer, LineDiffRendering.ChangeType(row));
        if (row.IsChanged)
        {
            var oldLine = row.OldIndex is { } oldIndex ? model.OldLines[oldIndex] : string.Empty;
            var newLine = row.NewIndex is { } newIndex ? model.NewLines[newIndex] : string.Empty;
            var spans = LineDiffRendering.ChangeSpans(oldLine, newLine, row);
            if (spans.Old.Count > 0 || spans.New.Count > 0)
            {
                WriteEmptyArray(writer);
                WriteEmptyArray(writer);
                WriteEmptyArray(writer);
                WriteSpans(writer, spans.Old);
                WriteSpans(writer, spans.New);
                WriteEmptyArray(writer);
            }
        }
        writer.WriteEndArray();
    }

    private static void WriteSpans(Utf8JsonWriter writer, IReadOnlyList<CommitFileDiffChangeSpan> spans)
    {
        writer.WriteStartArray();
        foreach (var span in spans)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(span.Start);
            writer.WriteNumberValue(span.Length);
            writer.WriteStringValue(span.ChangeType);
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }

    private static void WriteEmptyArray(Utf8JsonWriter writer)
    {
        writer.WriteStartArray();
        writer.WriteEndArray();
    }

    private static void WriteDelta(Utf8JsonWriter writer, int? value, ref int previous)
    {
        if (value is not { } number)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteNumberValue(number - previous);
        previous = number;
    }

    private static void WriteChangeType(Utf8JsonWriter writer, string changeType)
    {
        var code = changeType switch
        {
            "Unchanged" => 0,
            "Modified" => 1,
            "Deleted" => 2,
            "Inserted" => 3,
            "Added" => 4,
            "Imaginary" => 5,
            _ => -1,
        };
        if (code >= 0) writer.WriteNumberValue(code);
        else writer.WriteStringValue(changeType);
    }

    private readonly record struct CompactPayload(string Payload, int LineCount);
}
