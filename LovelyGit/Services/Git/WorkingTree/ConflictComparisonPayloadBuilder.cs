using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class ConflictComparisonPayloadBuilder
{
    private const int MinimumRows = 750;

    public static void Compact(CommitFileDiffResponse? response)
    {
        if (response is null || response.Lines.Count < MinimumRows)
        {
            return;
        }

        response.CompactLineSchema = "tuple-v4-delta-refs:gzip-base64:utf-8";
        response.CompactLinesGzipBase64 = Compress(response.Lines);
        response.CompactLineCount = response.Lines.Count;
        response.Lines = [];
    }

    public static CommitFileDiffResponse? BuildDirectIfUseful(
        string commitHash,
        string path,
        string status,
        LineDiffModel model,
        bool hasSyntaxHighlighting)
    {
        if (model.Rows.Count < MinimumRows || hasSyntaxHighlighting) return null;
        return ReferencedDiffPayloadBuilder.Build(
            commitHash,
            path,
            status,
            CommitDiffViewMode.SideBySide,
            model);
    }

    private static string Compress(IReadOnlyList<CommitFileDiffLine> lines)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        using (var writer = new Utf8JsonWriter(gzip))
        {
            writer.WriteStartArray();
            var previousOld = 0;
            var previousNew = 0;
            foreach (var line in lines)
            {
                WriteLine(writer, line, ref previousOld, ref previousNew);
            }
            writer.WriteEndArray();
        }

        return Convert.ToBase64String(output.GetBuffer(), 0, checked((int)output.Length));
    }

    private static void WriteLine(
        Utf8JsonWriter writer,
        CommitFileDiffLine line,
        ref int previousOld,
        ref int previousNew)
    {
        writer.WriteStartArray();
        WriteDelta(writer, line.OldLineNumber, ref previousOld);
        WriteDelta(writer, line.NewLineNumber, ref previousNew);
        WriteChangeType(writer, line.ChangeType);
        if (HasSpans(line)) WriteRenderingSpans(writer, line);
        writer.WriteEndArray();
    }

    private static bool HasSpans(CommitFileDiffLine line) =>
        HasValues(line.OldSyntaxSpans)
        || HasValues(line.NewSyntaxSpans)
        || HasValues(line.SyntaxSpans)
        || HasValues(line.OldChangeSpans)
        || HasValues(line.NewChangeSpans)
        || HasValues(line.ChangeSpans);

    private static bool HasValues<T>(IReadOnlyCollection<T>? values) => values is { Count: > 0 };

    private static void WriteRenderingSpans(Utf8JsonWriter writer, CommitFileDiffLine line)
    {
        WriteSpans(writer, line.OldSyntaxSpans, static (json, span) =>
            WriteSpan(json, span.Start, span.Length, span.Scope));
        WriteSpans(writer, line.NewSyntaxSpans, static (json, span) =>
            WriteSpan(json, span.Start, span.Length, span.Scope));
        WriteSpans(writer, line.SyntaxSpans, static (json, span) =>
            WriteSpan(json, span.Start, span.Length, span.Scope));
        WriteSpans(writer, line.OldChangeSpans, static (json, span) =>
            WriteSpan(json, span.Start, span.Length, span.ChangeType));
        WriteSpans(writer, line.NewChangeSpans, static (json, span) =>
            WriteSpan(json, span.Start, span.Length, span.ChangeType));
        WriteSpans(writer, line.ChangeSpans, static (json, span) =>
            WriteSpan(json, span.Start, span.Length, span.ChangeType));
    }

    private static void WriteSpans<T>(
        Utf8JsonWriter writer,
        IReadOnlyList<T>? spans,
        Action<Utf8JsonWriter, T> write)
    {
        writer.WriteStartArray();
        if (spans is not null)
        {
            foreach (var span in spans) write(writer, span);
        }
        writer.WriteEndArray();
    }

    private static void WriteSpan(Utf8JsonWriter writer, int start, int length, string value)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(start);
        writer.WriteNumberValue(length);
        writer.WriteStringValue(value);
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
}
