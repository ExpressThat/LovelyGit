using System.Text;

namespace LovelyGit.DiffBenchmarks;

internal static class DiffJsonLineWriter
{
    public static void Write(
        StringBuilder builder,
        DiffSerializationPlan plan,
        CommitDiffViewMode viewMode)
    {
        if (plan.OldText.Length == 0)
        {
            WriteSingle(builder, plan.NewText, viewMode, inserted: true);
        }
        else if (plan.NewText.Length == 0)
        {
            WriteSingle(builder, plan.OldText, viewMode, inserted: false);
        }
        else
        {
            WriteCompared(builder, plan.OldText, plan.NewText, viewMode);
        }
    }

    private static void WriteSingle(
        StringBuilder builder,
        string text,
        CommitDiffViewMode viewMode,
        bool inserted)
    {
        var cursor = new TextLineCursor(text);
        var number = 1;
        while (cursor.TryRead(out var line))
        {
            Row(builder, inserted ? null : number, inserted ? number : null, line, !inserted, line, inserted, inserted ? "Inserted" : "Deleted", viewMode);
            number++;
        }
    }

    private static void WriteCompared(
        StringBuilder builder,
        string oldText,
        string newText,
        CommitDiffViewMode viewMode)
    {
        var oldCursor = new TextLineCursor(oldText);
        var newCursor = new TextLineCursor(newText);
        var oldNumber = 1;
        var newNumber = 1;
        string? pendingOld = null;
        string? pendingNew = null;
        while (TryRead(ref oldCursor, ref pendingOld, out var oldLine))
        {
            if (!TryRead(ref newCursor, ref pendingNew, out var newLine))
            {
                Tail(builder, oldLine, ref oldNumber, oldCursor, viewMode, inserted: false);
                return;
            }

            if (oldLine.SequenceEqual(newLine))
            {
                Unchanged(builder, oldNumber++, newNumber++, oldLine, viewMode);
                continue;
            }

            Mismatch(builder, oldCursor, newCursor, ref pendingOld, ref pendingNew, ref oldNumber, ref newNumber, oldLine, newLine, viewMode);
        }

        if (TryRead(ref newCursor, ref pendingNew, out var remainingNew))
        {
            Tail(builder, remainingNew, ref newNumber, newCursor, viewMode, inserted: true);
        }
    }

    private static void Mismatch(
        StringBuilder builder,
        TextLineCursor oldCursor,
        TextLineCursor newCursor,
        ref string? pendingOld,
        ref string? pendingNew,
        ref int oldNumber,
        ref int newNumber,
        ReadOnlySpan<char> oldLine,
        ReadOnlySpan<char> newLine,
        CommitDiffViewMode viewMode)
    {
        if (oldCursor.Peek(out var nextOld) && nextOld.SequenceEqual(newLine))
        {
            pendingNew = newLine.ToString();
            Row(builder, oldNumber++, null, oldLine, true, default, false, "Deleted", viewMode);
        }
        else if (newCursor.Peek(out var nextNew) && oldLine.SequenceEqual(nextNew))
        {
            pendingOld = oldLine.ToString();
            Row(builder, null, newNumber++, default, false, newLine, true, "Inserted", viewMode);
        }
        else
        {
            Modified(builder, ref oldNumber, ref newNumber, oldLine, newLine, viewMode);
        }
    }

    private static void Modified(StringBuilder builder, ref int oldNumber, ref int newNumber, ReadOnlySpan<char> oldLine, ReadOnlySpan<char> newLine, CommitDiffViewMode viewMode)
    {
        if (viewMode == CommitDiffViewMode.SideBySide)
        {
            Row(builder, oldNumber++, newNumber++, oldLine, true, newLine, true, "Modified", viewMode);
        }
        else
        {
            Row(builder, oldNumber++, null, oldLine, true, default, false, "Deleted", viewMode);
            Row(builder, null, newNumber++, default, false, newLine, true, "Inserted", viewMode);
        }
    }

    private static void Unchanged(
        StringBuilder builder,
        int oldNumber,
        int newNumber,
        ReadOnlySpan<char> line,
        CommitDiffViewMode viewMode)
    {
        if (viewMode == CommitDiffViewMode.SideBySide)
        {
            DiffJsonStringWriter.UnchangedRow(builder, oldNumber, newNumber, line);
            return;
        }

        DiffJsonStringWriter.UnchangedRow(builder, oldNumber, newNumber, line);
    }

    private static void Tail(StringBuilder builder, ReadOnlySpan<char> first, ref int number, TextLineCursor cursor, CommitDiffViewMode viewMode, bool inserted)
    {
        Row(builder, inserted ? null : number, inserted ? number : null, first, !inserted, first, inserted, inserted ? "Inserted" : "Deleted", viewMode);
        number++;
        while (cursor.TryRead(out var line))
        {
            Row(builder, inserted ? null : number, inserted ? number : null, line, !inserted, line, inserted, inserted ? "Inserted" : "Deleted", viewMode);
            number++;
        }
    }

    private static bool TryRead(ref TextLineCursor cursor, ref string? pending, out ReadOnlySpan<char> line)
    {
        if (pending is not null)
        {
            line = pending;
            pending = null;
            return true;
        }

        return cursor.TryRead(out line);
    }

    private static void Row(StringBuilder builder, int? oldNumber, int? newNumber, ReadOnlySpan<char> oldText, bool hasOld, ReadOnlySpan<char> newText, bool hasNew, string changeType, CommitDiffViewMode viewMode)
    {
        DiffJsonStringWriter.RowStart(builder);
        DiffJsonStringWriter.NumberProperty(builder, "oldLineNumber", oldNumber);
        DiffJsonStringWriter.NumberProperty(builder, "newLineNumber", newNumber);
        if (viewMode == CommitDiffViewMode.SideBySide)
        {
            if (!hasOld && !hasNew)
            {
                DiffJsonStringWriter.TextProperty(builder, "text", newText);
            }
            else
            {
                if (hasOld) DiffJsonStringWriter.TextProperty(builder, "oldText", oldText);
                if (hasNew) DiffJsonStringWriter.TextProperty(builder, "newText", newText);
            }
        }
        else
        {
            DiffJsonStringWriter.TextProperty(builder, "text", hasNew ? newText : oldText);
        }

        DiffJsonStringWriter.ChangeType(builder, changeType);
        builder.Append('}');
    }
}
