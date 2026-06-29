using System.Text.Json;

namespace LovelyGit.DiffBenchmarks;

internal static partial class PlannedDiffJsonWriter
{
    public static void WriteLines(
        Utf8JsonWriter writer,
        DiffSerializationPlan plan,
        CommitDiffViewMode viewMode)
    {
        if (plan.OldText.Length == 0)
        {
            WriteSingleSided(writer, plan.NewText, viewMode, inserted: true);
            return;
        }

        if (plan.NewText.Length == 0)
        {
            WriteSingleSided(writer, plan.OldText, viewMode, inserted: false);
            return;
        }

        WriteResynced(writer, plan.OldText, plan.NewText, viewMode);
    }

    private static void WriteSingleSided(
        Utf8JsonWriter writer,
        string text,
        CommitDiffViewMode viewMode,
        bool inserted)
    {
        var cursor = new TextLineCursor(text);
        var lineNumber = 1;
        while (cursor.TryRead(out var line))
        {
            WriteRow(
                writer,
                inserted ? null : lineNumber,
                inserted ? lineNumber : null,
                line,
                hasOldText: !inserted,
                line,
                hasNewText: inserted,
                inserted ? "Inserted" : "Deleted",
                viewMode);
            lineNumber++;
        }
    }

    private static void WriteResynced(
        Utf8JsonWriter writer,
        string oldText,
        string newText,
        CommitDiffViewMode viewMode)
    {
        var oldCursor = new TextLineCursor(oldText);
        var newCursor = new TextLineCursor(newText);
        var oldLineNumber = 1;
        var newLineNumber = 1;
        string? pendingOld = null;
        string? pendingNew = null;
        while (TryRead(ref oldCursor, ref pendingOld, out var oldLine))
        {
            if (!TryRead(ref newCursor, ref pendingNew, out var newLine))
            {
                WriteTail(writer, oldLine, ref oldLineNumber, oldCursor, viewMode, inserted: false);
                return;
            }

            if (oldLine.SequenceEqual(newLine))
            {
                WriteUnchanged(writer, oldLineNumber++, newLineNumber++, oldLine, viewMode);
                continue;
            }

            WriteMismatch(writer, oldCursor, newCursor, ref pendingOld, ref pendingNew, ref oldLineNumber, ref newLineNumber, oldLine, newLine, viewMode);
        }

        if (TryRead(ref newCursor, ref pendingNew, out var remainingNewLine))
        {
            WriteTail(writer, remainingNewLine, ref newLineNumber, newCursor, viewMode, inserted: true);
        }
    }

    private static void WriteMismatch(
        Utf8JsonWriter writer,
        TextLineCursor oldCursor,
        TextLineCursor newCursor,
        ref string? pendingOld,
        ref string? pendingNew,
        ref int oldLineNumber,
        ref int newLineNumber,
        ReadOnlySpan<char> oldLine,
        ReadOnlySpan<char> newLine,
        CommitDiffViewMode viewMode)
    {
        if (oldCursor.Peek(out var nextOld) && nextOld.SequenceEqual(newLine))
        {
            pendingNew = newLine.ToString();
            WriteRow(writer, oldLineNumber++, null, oldLine, hasOldText: true, default, hasNewText: false, "Deleted", viewMode);
        }
        else if (newCursor.Peek(out var nextNew) && oldLine.SequenceEqual(nextNew))
        {
            pendingOld = oldLine.ToString();
            WriteRow(writer, null, newLineNumber++, default, hasOldText: false, newLine, hasNewText: true, "Inserted", viewMode);
        }
        else
        {
            WriteModified(writer, ref oldLineNumber, ref newLineNumber, oldLine, newLine, viewMode);
        }
    }

    private static void WriteModified(
        Utf8JsonWriter writer,
        ref int oldLineNumber,
        ref int newLineNumber,
        ReadOnlySpan<char> oldLine,
        ReadOnlySpan<char> newLine,
        CommitDiffViewMode viewMode)
    {
        if (viewMode == CommitDiffViewMode.SideBySide)
        {
            WriteRow(writer, oldLineNumber++, newLineNumber++, oldLine, true, newLine, true, "Modified", viewMode);
            return;
        }

        WriteRow(writer, oldLineNumber++, null, oldLine, true, default, false, "Deleted", viewMode);
        WriteRow(writer, null, newLineNumber++, default, false, newLine, true, "Inserted", viewMode);
    }

    private static void WriteUnchanged(
        Utf8JsonWriter writer,
        int oldLineNumber,
        int newLineNumber,
        ReadOnlySpan<char> line,
        CommitDiffViewMode viewMode)
    {
        if (viewMode == CommitDiffViewMode.SideBySide)
        {
            WriteRow(writer, oldLineNumber, newLineNumber, default, false, line, false, "Unchanged", viewMode);
            return;
        }

        WriteRow(writer, oldLineNumber, newLineNumber, line, true, line, true, "Unchanged", viewMode);
    }

    private static bool TryRead(
        ref TextLineCursor cursor,
        ref string? pending,
        out ReadOnlySpan<char> line)
    {
        if (pending is not null)
        {
            line = pending;
            pending = null;
            return true;
        }

        return cursor.TryRead(out line);
    }

    private static void WriteTail(
        Utf8JsonWriter writer,
        ReadOnlySpan<char> firstLine,
        ref int lineNumber,
        TextLineCursor cursor,
        CommitDiffViewMode viewMode,
        bool inserted)
    {
        WriteRow(
            writer,
            inserted ? null : lineNumber,
            inserted ? lineNumber : null,
            firstLine,
            hasOldText: !inserted,
            firstLine,
            hasNewText: inserted,
            inserted ? "Inserted" : "Deleted",
            viewMode);
        lineNumber++;
        while (cursor.TryRead(out var line))
        {
            WriteRow(
                writer,
                inserted ? null : lineNumber,
                inserted ? lineNumber : null,
                line,
                hasOldText: !inserted,
                line,
                hasNewText: inserted,
                inserted ? "Inserted" : "Deleted",
                viewMode);
            lineNumber++;
        }
    }

}
