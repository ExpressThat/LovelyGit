using System.Buffers;

namespace LovelyGit.DiffBenchmarks;

internal static class DirectDiffJsonLengthCounter
{
    private static readonly SearchValues<char> EscapeCharacters =
        SearchValues.Create(DiffJsonEscapeCharacters.Values);

    public static int Count(CommitFileDiffResponse response)
    {
        var count = 1;
        count += Property("commitHash", response.CommitHash) + 1;
        count += Property("path", response.Path) + 1;
        count += Property("status", response.Status) + 1;
        count += Property("viewMode", response.ViewMode.ToString()) + 1;
        count += "\"isBinary\":false,".Length;
        count += "\"hasDifferences\":".Length + (response.HasDifferences ? 4 : 5) + 1;
        count += "\"isTruncated\":false,".Length;
        count += Property("truncationMessage", string.Empty) + 1;
        count += "\"lines\":[".Length;
        count += Lines(response.Plan!, response.ViewMode);
        return count + 2;
    }

    private static int Lines(DiffSerializationPlan plan, CommitDiffViewMode viewMode)
    {
        if (plan.OldText.Length == 0)
        {
            return Single(plan.NewText, viewMode, inserted: true);
        }

        if (plan.NewText.Length == 0)
        {
            return Single(plan.OldText, viewMode, inserted: false);
        }

        return Compared(plan.OldText, plan.NewText, viewMode);
    }

    private static int Single(string text, CommitDiffViewMode viewMode, bool inserted)
    {
        var count = 0;
        var cursor = new TextLineCursor(text);
        var number = 1;
        while (cursor.TryRead(out var line))
        {
            count += RowSeparator(count) + Row(inserted ? null : number, inserted ? number : null, line, !inserted, line, inserted, inserted ? "Inserted" : "Deleted", viewMode);
            number++;
        }

        return count;
    }

    private static int Compared(string oldText, string newText, CommitDiffViewMode viewMode)
    {
        var oldCursor = new TextLineCursor(oldText);
        var newCursor = new TextLineCursor(newText);
        var oldNumber = 1;
        var newNumber = 1;
        var rows = 0;
        string? pendingOld = null;
        string? pendingNew = null;
        while (TryRead(ref oldCursor, ref pendingOld, out var oldLine))
        {
            if (!TryRead(ref newCursor, ref pendingNew, out var newLine))
            {
                return rows + Tail(oldLine, ref oldNumber, oldCursor, viewMode, inserted: false, hasRows: rows > 0);
            }

            if (oldLine.SequenceEqual(newLine))
            {
                rows += RowSeparator(rows) + Unchanged(oldNumber++, newNumber++, oldLine);
                continue;
            }

            rows += Mismatch(oldCursor, newCursor, ref pendingOld, ref pendingNew, ref oldNumber, ref newNumber, oldLine, newLine, viewMode, rows > 0);
        }

        return TryRead(ref newCursor, ref pendingNew, out var remainingNew)
            ? rows + Tail(remainingNew, ref newNumber, newCursor, viewMode, inserted: true, hasRows: rows > 0)
            : rows;
    }

    private static int Mismatch(TextLineCursor oldCursor, TextLineCursor newCursor, ref string? pendingOld, ref string? pendingNew, ref int oldNumber, ref int newNumber, ReadOnlySpan<char> oldLine, ReadOnlySpan<char> newLine, CommitDiffViewMode viewMode, bool hasRows)
    {
        if (oldCursor.Peek(out var nextOld) && nextOld.SequenceEqual(newLine))
        {
            pendingNew = newLine.ToString();
            return RowSeparator(hasRows) + Row(oldNumber++, null, oldLine, true, default, false, "Deleted", viewMode);
        }

        if (newCursor.Peek(out var nextNew) && oldLine.SequenceEqual(nextNew))
        {
            pendingOld = oldLine.ToString();
            return RowSeparator(hasRows) + Row(null, newNumber++, default, false, newLine, true, "Inserted", viewMode);
        }

        return Modified(ref oldNumber, ref newNumber, oldLine, newLine, viewMode, hasRows);
    }

    private static int Modified(ref int oldNumber, ref int newNumber, ReadOnlySpan<char> oldLine, ReadOnlySpan<char> newLine, CommitDiffViewMode viewMode, bool hasRows)
    {
        if (viewMode == CommitDiffViewMode.SideBySide)
        {
            return RowSeparator(hasRows) + Row(oldNumber++, newNumber++, oldLine, true, newLine, true, "Modified", viewMode);
        }

        return RowSeparator(hasRows) + Row(oldNumber++, null, oldLine, true, default, false, "Deleted", viewMode)
            + RowSeparator(true) + Row(null, newNumber++, default, false, newLine, true, "Inserted", viewMode);
    }

    private static int Tail(ReadOnlySpan<char> first, ref int number, TextLineCursor cursor, CommitDiffViewMode viewMode, bool inserted, bool hasRows)
    {
        var count = RowSeparator(hasRows) + Row(inserted ? null : number, inserted ? number : null, first, !inserted, first, inserted, inserted ? "Inserted" : "Deleted", viewMode);
        number++;
        while (cursor.TryRead(out var line))
        {
            count += RowSeparator(true) + Row(inserted ? null : number, inserted ? number : null, line, !inserted, line, inserted, inserted ? "Inserted" : "Deleted", viewMode);
            number++;
        }

        return count;
    }

    private static int Row(int? oldNumber, int? newNumber, ReadOnlySpan<char> oldText, bool hasOld, ReadOnlySpan<char> newText, bool hasNew, string changeType, CommitDiffViewMode viewMode)
    {
        var count = 1;
        var hasPrevious = false;
        count += Number("oldLineNumber", oldNumber, hasPrevious);
        hasPrevious |= oldNumber.HasValue;
        count += Number("newLineNumber", newNumber, hasPrevious);
        hasPrevious |= newNumber.HasValue;
        count += viewMode == CommitDiffViewMode.SideBySide
            ? SideBySideText(oldText, hasOld, newText, hasNew, hasPrevious)
            : Text("text", hasNew ? newText : oldText, hasPrevious);
        return count + Property("changeType", changeType, hasPrevious: true) + 1;
    }

    private static int Unchanged(int oldNumber, int newNumber, ReadOnlySpan<char> line) =>
        2 + Number("oldLineNumber", oldNumber, hasPrevious: false)
        + Number("newLineNumber", newNumber, hasPrevious: true)
        + Text("text", line, hasPrevious: true)
        + Property("changeType", "Unchanged", hasPrevious: true);

    private static int SideBySideText(ReadOnlySpan<char> oldText, bool hasOld, ReadOnlySpan<char> newText, bool hasNew, bool hasPrevious)
    {
        if (!hasOld && !hasNew)
        {
            return Text("text", newText, hasPrevious);
        }

        var count = 0;
        if (hasOld)
        {
            count += Text("oldText", oldText, hasPrevious);
            hasPrevious = true;
        }

        if (hasNew)
        {
            count += Text("newText", newText, hasPrevious);
        }

        return count;
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

    private static int RowSeparator(int count) => count == 0 ? 0 : 1;
    private static int RowSeparator(bool hasRows) => hasRows ? 1 : 0;
    private static int Number(string name, int? value, bool hasPrevious) => value.HasValue ? CommaPrefix(hasPrevious) + name.Length + 3 + Digits(value.Value) : 0;
    private static int Property(string name, string value, bool hasPrevious = false) => CommaPrefix(hasPrevious) + name.Length + 3 + Escaped(value);
    private static int Text(string name, ReadOnlySpan<char> value, bool hasPrevious) => CommaPrefix(hasPrevious) + name.Length + 3 + Escaped(value);
    private static int CommaPrefix(bool hasPrevious) => hasPrevious ? 1 : 0;
    private static int Escaped(ReadOnlySpan<char> value)
    {
        var offset = value.IndexOfAny(EscapeCharacters);
        if (offset < 0)
        {
            return value.Length + 2;
        }

        var extra = 0;
        for (var index = offset; index < value.Length; index++)
        {
            var ch = value[index];
            if (ch is '"' or '\\' or '\b' or '\f' or '\n' or '\r' or '\t')
            {
                extra++;
            }
            else if (ch < ' ')
            {
                extra += 5;
            }
        }

        return value.Length + 2 + extra;
    }

    private static int Digits(int value) => value < 10 ? 1 : value < 100 ? 2 : value < 1000 ? 3 : value < 10000 ? 4 : value < 100000 ? 5 : value < 1000000 ? 6 : value < 10000000 ? 7 : 8;
}
