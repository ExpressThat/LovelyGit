namespace LovelyGit.DiffBenchmarks;

internal static class LovelyGitStreamingResync
{
    public static bool TryRun(
        string oldText,
        string newText,
        int lineCount,
        CommitDiffViewMode viewMode,
        StringComparer comparer,
        out CommitFileDiffResponse response)
    {
        var oldCursor = new TextLineCursor(oldText);
        var newCursor = new TextLineCursor(newText);
        var rows = new List<CommitFileDiffLine>(lineCount + 8);
        var oldLine = 1;
        var newLine = 1;
        var hasDifferences = false;
        string? pendingOld = null;
        string? pendingNew = null;
        while (TryReadLine(ref oldCursor, ref pendingOld, out var oldTextLine))
        {
            if (!TryReadLine(ref newCursor, ref pendingNew, out var newTextLine))
            {
                AddTail(rows, oldTextLine, ref oldLine, oldCursor, viewMode, inserted: false);
                response = Response(viewMode, rows, hasDifferences: true);
                return true;
            }

            if (LineEquals(oldTextLine, newTextLine, comparer))
            {
                rows.Add(Unchanged(oldLine, newLine, oldTextLine.ToString(), viewMode));
                oldLine++;
                newLine++;
                continue;
            }

            ResolveMismatch(
                rows,
                oldCursor,
                newCursor,
                ref pendingOld,
                ref pendingNew,
                ref oldLine,
                ref newLine,
                oldTextLine,
                newTextLine,
                viewMode,
                comparer);
            hasDifferences = true;
        }

        if (TryReadLine(ref newCursor, ref pendingNew, out var remainingNewLine))
        {
            AddTail(rows, remainingNewLine, ref newLine, newCursor, viewMode, inserted: true);
            hasDifferences = true;
        }

        response = Response(viewMode, rows, hasDifferences);
        return true;
    }

    private static void ResolveMismatch(
        List<CommitFileDiffLine> rows,
        TextLineCursor oldCursor,
        TextLineCursor newCursor,
        ref string? pendingOld,
        ref string? pendingNew,
        ref int oldLine,
        ref int newLine,
        string oldText,
        string newText,
        CommitDiffViewMode viewMode,
        StringComparer comparer)
    {
        if (oldCursor.Peek(out var nextOld) && LineEquals(nextOld, newText, comparer))
        {
            pendingNew = newText;
            rows.Add(Row(oldLine++, null, oldText, null, "Deleted", viewMode));
        }
        else if (newCursor.Peek(out var nextNew) && LineEquals(oldText, nextNew, comparer))
        {
            pendingOld = oldText;
            rows.Add(Row(null, newLine++, null, newText, "Inserted", viewMode));
        }
        else
        {
            rows.Add(Row(oldLine++, newLine++, oldText, newText, "Modified", viewMode));
        }
    }

    private static bool TryReadLine(ref TextLineCursor cursor, ref string? pending, out string line)
    {
        if (pending is not null)
        {
            line = pending;
            pending = null;
            return true;
        }

        if (cursor.TryRead(out var span))
        {
            line = span.ToString();
            return true;
        }

        line = string.Empty;
        return false;
    }

    private static void AddTail(
        List<CommitFileDiffLine> rows,
        ReadOnlySpan<char> firstLine,
        ref int lineNumber,
        TextLineCursor cursor,
        CommitDiffViewMode viewMode,
        bool inserted)
    {
        AddTailRow(rows, firstLine, ref lineNumber, viewMode, inserted);
        while (cursor.TryRead(out var line))
        {
            AddTailRow(rows, line, ref lineNumber, viewMode, inserted);
        }
    }

    private static void AddTailRow(
        List<CommitFileDiffLine> rows,
        ReadOnlySpan<char> line,
        ref int lineNumber,
        CommitDiffViewMode viewMode,
        bool inserted)
    {
        rows.Add(inserted
            ? Row(null, lineNumber++, null, line.ToString(), "Inserted", viewMode)
            : Row(lineNumber++, null, line.ToString(), null, "Deleted", viewMode));
    }

    private static CommitFileDiffLine Unchanged(int oldLine, int newLine, string text, CommitDiffViewMode viewMode) =>
        viewMode == CommitDiffViewMode.SideBySide
            ? new CommitFileDiffLine { OldLineNumber = oldLine, NewLineNumber = newLine, OldText = text, NewText = text, ChangeType = "Unchanged" }
            : Row(oldLine, newLine, text, text, "Unchanged", viewMode);

    private static CommitFileDiffLine Row(int? oldLine, int? newLine, string? oldText, string? newText, string changeType, CommitDiffViewMode viewMode) =>
        viewMode == CommitDiffViewMode.SideBySide
            ? new CommitFileDiffLine { OldLineNumber = oldLine, NewLineNumber = newLine, OldText = oldText, NewText = newText, ChangeType = changeType }
            : new CommitFileDiffLine { OldLineNumber = oldLine, NewLineNumber = newLine, Text = newText ?? oldText ?? string.Empty, ChangeType = changeType };

    private static CommitFileDiffResponse Response(CommitDiffViewMode viewMode, List<CommitFileDiffLine> rows, bool hasDifferences) =>
        new()
        {
            CommitHash = "LovelyGit Prototype",
            Path = "benchmark.txt",
            Status = "Modified",
            ViewMode = viewMode,
            HasDifferences = hasDifferences,
            Lines = rows,
        };

    private static bool LineEquals(ReadOnlySpan<char> oldLine, ReadOnlySpan<char> newLine, StringComparer comparer) =>
        ReferenceEquals(comparer, StringComparer.Ordinal)
            ? oldLine.SequenceEqual(newLine)
            : comparer.Equals(oldLine.ToString(), newLine.ToString());
}
