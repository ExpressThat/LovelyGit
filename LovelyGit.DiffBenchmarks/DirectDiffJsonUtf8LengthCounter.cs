using System.Buffers;
using System.Text;

namespace LovelyGit.DiffBenchmarks;

internal static class DirectDiffJsonUtf8LengthCounter
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
        count += Property("lineSchema", "tuple-v1") + 1;
        count += "\"lines\":[".Length;
        count += Lines(response.Plan!, response.ViewMode);
        return count + 2;
    }

    private static int Lines(DiffSerializationPlan plan, CommitDiffViewMode viewMode)
    {
        if (plan.OldText.Length == 0)
        {
            return Single(plan.NewText, viewMode, inserted: true, plan.IsAscii);
        }

        if (plan.NewText.Length == 0)
        {
            return Single(plan.OldText, viewMode, inserted: false, plan.IsAscii);
        }

        return Compared(plan.OldText, plan.NewText, viewMode, plan.IsAscii);
    }

    private static int Single(string text, CommitDiffViewMode viewMode, bool inserted, bool isAscii)
    {
        var count = 0;
        var cursor = new TextLineCursor(text);
        var number = 1;
        while (cursor.TryRead(out var line))
        {
            count += RowSeparator(count) + Row(inserted ? null : number, inserted ? number : null, line, !inserted, line, inserted, inserted ? "Inserted" : "Deleted", viewMode, isAscii);
            number++;
        }

        return count;
    }

    private static int Compared(string oldText, string newText, CommitDiffViewMode viewMode, bool isAscii)
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
                return rows + Tail(oldLine, ref oldNumber, oldCursor, viewMode, inserted: false, hasRows: rows > 0, isAscii);
            }

            if (oldLine.SequenceEqual(newLine))
            {
                rows += RowSeparator(rows) + Unchanged(oldNumber++, newNumber++, oldLine, isAscii);
                continue;
            }

            rows += Mismatch(oldCursor, newCursor, ref pendingOld, ref pendingNew, ref oldNumber, ref newNumber, oldLine, newLine, viewMode, rows > 0, isAscii);
        }

        return TryRead(ref newCursor, ref pendingNew, out var remainingNew)
            ? rows + Tail(remainingNew, ref newNumber, newCursor, viewMode, inserted: true, hasRows: rows > 0, isAscii)
            : rows;
    }

    private static int Mismatch(TextLineCursor oldCursor, TextLineCursor newCursor, ref string? pendingOld, ref string? pendingNew, ref int oldNumber, ref int newNumber, ReadOnlySpan<char> oldLine, ReadOnlySpan<char> newLine, CommitDiffViewMode viewMode, bool hasRows, bool isAscii)
    {
        if (oldCursor.Peek(out var nextOld) && nextOld.SequenceEqual(newLine))
        {
            pendingNew = newLine.ToString();
            return RowSeparator(hasRows) + Row(oldNumber++, null, oldLine, true, default, false, "Deleted", viewMode, isAscii);
        }

        if (newCursor.Peek(out var nextNew) && oldLine.SequenceEqual(nextNew))
        {
            pendingOld = oldLine.ToString();
            return RowSeparator(hasRows) + Row(null, newNumber++, default, false, newLine, true, "Inserted", viewMode, isAscii);
        }

        return Modified(ref oldNumber, ref newNumber, oldLine, newLine, viewMode, hasRows, isAscii);
    }

    private static int Modified(ref int oldNumber, ref int newNumber, ReadOnlySpan<char> oldLine, ReadOnlySpan<char> newLine, CommitDiffViewMode viewMode, bool hasRows, bool isAscii)
    {
        if (viewMode == CommitDiffViewMode.SideBySide)
        {
            return RowSeparator(hasRows) + Row(oldNumber++, newNumber++, oldLine, true, newLine, true, "Modified", viewMode, isAscii);
        }

        return RowSeparator(hasRows) + Row(oldNumber++, null, oldLine, true, default, false, "Deleted", viewMode, isAscii)
            + RowSeparator(true) + Row(null, newNumber++, default, false, newLine, true, "Inserted", viewMode, isAscii);
    }

    private static int Tail(ReadOnlySpan<char> first, ref int number, TextLineCursor cursor, CommitDiffViewMode viewMode, bool inserted, bool hasRows, bool isAscii)
    {
        var count = RowSeparator(hasRows) + Row(inserted ? null : number, inserted ? number : null, first, !inserted, first, inserted, inserted ? "Inserted" : "Deleted", viewMode, isAscii);
        number++;
        while (cursor.TryRead(out var line))
        {
            count += RowSeparator(true) + Row(inserted ? null : number, inserted ? number : null, line, !inserted, line, inserted, inserted ? "Inserted" : "Deleted", viewMode, isAscii);
            number++;
        }

        return count;
    }

    private static int Row(int? oldNumber, int? newNumber, ReadOnlySpan<char> oldText, bool hasOld, ReadOnlySpan<char> newText, bool hasNew, string changeType, CommitDiffViewMode viewMode, bool isAscii)
    {
        var count = 1 + NumberOrNull(oldNumber) + 1 + NumberOrNull(newNumber) + 3;
        count += viewMode == CommitDiffViewMode.SideBySide
            ? SideBySideText(oldText, hasOld, newText, hasNew, isAscii)
            : Escaped(hasNew ? newText : oldText, isAscii);
        return count + 1;
    }

    private static int Unchanged(int oldNumber, int newNumber, ReadOnlySpan<char> line, bool isAscii) =>
        1 + Digits(oldNumber) + 1 + Digits(newNumber) + 3 + Escaped(line, isAscii) + 1;

    private static int SideBySideText(ReadOnlySpan<char> oldText, bool hasOld, ReadOnlySpan<char> newText, bool hasNew, bool isAscii)
    {
        return (hasOld ? Escaped(oldText, isAscii) : 4)
            + 1
            + (hasNew ? Escaped(newText, isAscii) : 4);
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
    private static int NumberOrNull(int? value) => value.HasValue ? Digits(value.Value) : 4;
    private static int Property(string name, string value, bool hasPrevious = false) => CommaPrefix(hasPrevious) + name.Length + 3 + Escaped(value, isAscii: true);
    private static int Text(string name, ReadOnlySpan<char> value, bool hasPrevious, bool isAscii) => CommaPrefix(hasPrevious) + name.Length + 3 + Escaped(value, isAscii);
    private static int CommaPrefix(bool hasPrevious) => hasPrevious ? 1 : 0;
    private static int Digits(int value) => value < 10 ? 1 : value < 100 ? 2 : value < 1000 ? 3 : value < 10000 ? 4 : value < 100000 ? 5 : value < 1000000 ? 6 : value < 10000000 ? 7 : value < 100000000 ? 8 : value < 1000000000 ? 9 : 10;

    private static int Escaped(ReadOnlySpan<char> value, bool isAscii)
    {
        var offset = value.IndexOfAny(EscapeCharacters);
        if (offset < 0)
        {
            return Utf8Length(value, isAscii) + 2;
        }

        var count = 2;
        var start = 0;
        while (start < value.Length)
        {
            count += Utf8Length(value[start..offset], isAscii);
            count += value[offset] < ' ' && value[offset] is not '\b' and not '\f' and not '\n' and not '\r' and not '\t' ? 6 : 2;
            start = offset + 1;
            var next = value[start..].IndexOfAny(EscapeCharacters);
            if (next < 0)
            {
                return count + Utf8Length(value[start..], isAscii);
            }

            offset = start + next;
        }

        return count;
    }

    private static int Utf8Length(ReadOnlySpan<char> value, bool isAscii)
    {
        if (isAscii)
        {
            return value.Length;
        }

        foreach (var ch in value)
        {
            if (ch > 0x7f)
            {
                return Encoding.UTF8.GetByteCount(value);
            }
        }

        return value.Length;
    }
}
