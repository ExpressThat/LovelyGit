using System.Text.Json;
using System.Text.Json.Serialization;

namespace LovelyGit.DiffBenchmarks;

internal sealed class CommitFileDiffResponseJsonConverter : JsonConverter<CommitFileDiffResponse>
{
    private static readonly JsonEncodedText ChangeType = JsonEncodedText.Encode("changeType");
    private static readonly JsonEncodedText CommitHash = JsonEncodedText.Encode("commitHash");
    private static readonly JsonEncodedText HasDifferences = JsonEncodedText.Encode("hasDifferences");
    private static readonly JsonEncodedText IsBinary = JsonEncodedText.Encode("isBinary");
    private static readonly JsonEncodedText IsTruncated = JsonEncodedText.Encode("isTruncated");
    private static readonly JsonEncodedText Lines = JsonEncodedText.Encode("lines");
    private static readonly JsonEncodedText NewLineNumber = JsonEncodedText.Encode("newLineNumber");
    private static readonly JsonEncodedText NewText = JsonEncodedText.Encode("newText");
    private static readonly JsonEncodedText OldLineNumber = JsonEncodedText.Encode("oldLineNumber");
    private static readonly JsonEncodedText OldText = JsonEncodedText.Encode("oldText");
    private static readonly JsonEncodedText Path = JsonEncodedText.Encode("path");
    private static readonly JsonEncodedText Status = JsonEncodedText.Encode("status");
    private static readonly JsonEncodedText Text = JsonEncodedText.Encode("text");
    private static readonly JsonEncodedText TruncationMessage = JsonEncodedText.Encode("truncationMessage");
    private static readonly JsonEncodedText ViewMode = JsonEncodedText.Encode("viewMode");

    public override CommitFileDiffResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        throw new NotSupportedException("Benchmark responses are write-only.");

    public override void Write(
        Utf8JsonWriter writer,
        CommitFileDiffResponse value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        WriteResponsePrefix(writer, value);
        writer.WritePropertyName(Lines);
        writer.WriteStartArray();
        if (value.Plan is null)
        {
            WriteMaterializedLines(writer, value.Lines);
        }
        else
        {
            PlannedDiffJsonWriter.WriteLines(writer, value.Plan, value.ViewMode);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteMaterializedLines(
        Utf8JsonWriter writer,
        IReadOnlyList<CommitFileDiffLine> lines)
    {
        foreach (var line in lines)
        {
            writer.WriteStartObject();
            WriteLineNumber(writer, OldLineNumber, line.OldLineNumber);
            WriteLineNumber(writer, NewLineNumber, line.NewLineNumber);
            WriteString(writer, OldText, line.OldText);
            WriteString(writer, NewText, line.NewText);
            WriteString(writer, Text, line.Text);
            writer.WriteString(ChangeType, line.ChangeType);
            writer.WriteEndObject();
        }
    }

    internal static void WriteResponsePrefix(
        Utf8JsonWriter writer,
        CommitFileDiffResponse value)
    {
        writer.WriteString(CommitHash, value.CommitHash);
        writer.WriteString(Path, value.Path);
        writer.WriteString(Status, value.Status);
        writer.WriteString(ViewMode, value.ViewMode.ToString());
        writer.WriteBoolean(IsBinary, value.IsBinary);
        writer.WriteBoolean(HasDifferences, value.HasDifferences);
        writer.WriteBoolean(IsTruncated, value.IsTruncated);
        writer.WriteString(TruncationMessage, value.TruncationMessage);
    }

    internal static void WriteLineNumber(Utf8JsonWriter writer, JsonEncodedText name, int? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(name, value.Value);
        }
    }

    internal static void WriteString(Utf8JsonWriter writer, JsonEncodedText name, string? value)
    {
        if (value is not null)
        {
            writer.WriteString(name, value);
        }
    }

    internal static void WriteString(Utf8JsonWriter writer, JsonEncodedText name, ReadOnlySpan<char> value)
    {
        writer.WritePropertyName(name);
        writer.WriteStringValue(value);
    }

    internal static JsonEncodedText ChangeTypeName => ChangeType;
    internal static JsonEncodedText NewLineNumberName => NewLineNumber;
    internal static JsonEncodedText NewTextName => NewText;
    internal static JsonEncodedText OldLineNumberName => OldLineNumber;
    internal static JsonEncodedText OldTextName => OldText;
    internal static JsonEncodedText TextName => Text;
}
