using System.Text.Json;

namespace LovelyGit.DiffBenchmarks;

internal static partial class PlannedDiffJsonWriter
{
    private static void WriteRow(
        Utf8JsonWriter writer,
        int? oldNumber,
        int? newNumber,
        ReadOnlySpan<char> oldText,
        bool hasOldText,
        ReadOnlySpan<char> newText,
        bool hasNewText,
        string changeType,
        CommitDiffViewMode viewMode)
    {
        writer.WriteStartObject();
        CommitFileDiffResponseJsonConverter.WriteLineNumber(
            writer,
            CommitFileDiffResponseJsonConverter.OldLineNumberName,
            oldNumber);
        CommitFileDiffResponseJsonConverter.WriteLineNumber(
            writer,
            CommitFileDiffResponseJsonConverter.NewLineNumberName,
            newNumber);
        WriteText(writer, oldText, hasOldText, newText, hasNewText, viewMode);
        writer.WriteString(CommitFileDiffResponseJsonConverter.ChangeTypeName, changeType);
        writer.WriteEndObject();
    }

    private static void WriteText(
        Utf8JsonWriter writer,
        ReadOnlySpan<char> oldText,
        bool hasOldText,
        ReadOnlySpan<char> newText,
        bool hasNewText,
        CommitDiffViewMode viewMode)
    {
        if (viewMode != CommitDiffViewMode.SideBySide)
        {
            CommitFileDiffResponseJsonConverter.WriteString(
                writer,
                CommitFileDiffResponseJsonConverter.TextName,
                hasNewText ? newText : oldText);
            return;
        }

        if (!hasOldText && !hasNewText)
        {
            CommitFileDiffResponseJsonConverter.WriteString(
                writer,
                CommitFileDiffResponseJsonConverter.TextName,
                newText);
            return;
        }

        WriteSideBySideText(writer, oldText, hasOldText, newText, hasNewText);
    }

    private static void WriteSideBySideText(
        Utf8JsonWriter writer,
        ReadOnlySpan<char> oldText,
        bool hasOldText,
        ReadOnlySpan<char> newText,
        bool hasNewText)
    {
        if (hasOldText)
        {
            CommitFileDiffResponseJsonConverter.WriteString(
                writer,
                CommitFileDiffResponseJsonConverter.OldTextName,
                oldText);
        }

        if (hasNewText)
        {
            CommitFileDiffResponseJsonConverter.WriteString(
                writer,
                CommitFileDiffResponseJsonConverter.NewTextName,
                newText);
        }
    }
}
