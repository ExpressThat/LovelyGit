namespace LovelyGit.DiffBenchmarks;

internal ref partial struct DirectDiffJsonUtf8SpanWriter(Span<byte> output, bool assumeAscii)
{
    private Span<byte> remaining = output;
    private readonly bool assumeAscii = assumeAscii;

    public void Response(CommitFileDiffResponse response)
    {
        Raw('{');
        Property("commitHash", response.CommitHash);
        Raw(',');
        Property("path", response.Path);
        Raw(',');
        Property("status", response.Status);
        Raw(',');
        Property("viewMode", response.ViewMode.ToString());
        Raw(",\"isBinary\":false,\"hasDifferences\":");
        Raw(response.HasDifferences ? "true" : "false");
        Raw(",\"isTruncated\":false,");
        Property("truncationMessage", string.Empty);
        Raw(",\"lineSchema\":\"tuple-v1\",\"lines\":[");
        Lines(response.Plan!, response.ViewMode);
        Raw("]}");
    }

    private void Lines(DiffSerializationPlan plan, CommitDiffViewMode viewMode)
    {
        if (plan.OldText.Length == 0)
        {
            Single(plan.NewText, viewMode, inserted: true);
            return;
        }

        if (plan.NewText.Length == 0)
        {
            Single(plan.OldText, viewMode, inserted: false);
            return;
        }

        Compared(plan.OldText, plan.NewText, viewMode);
    }

    private void Single(string text, CommitDiffViewMode viewMode, bool inserted)
    {
        var cursor = new TextLineCursor(text);
        var number = 1;
        var hasRows = false;
        while (cursor.TryRead(out var line))
        {
            RowSeparator(hasRows);
            Row(inserted ? null : number, inserted ? number : null, line, !inserted, line, inserted, inserted ? "Inserted" : "Deleted", viewMode);
            hasRows = true;
            number++;
        }
    }

    private void Compared(string oldText, string newText, CommitDiffViewMode viewMode)
    {
        var oldCursor = new TextLineCursor(oldText);
        var newCursor = new TextLineCursor(newText);
        var oldNumber = 1;
        var newNumber = 1;
        var hasRows = false;
        string? pendingOld = null;
        string? pendingNew = null;
        while (TryRead(ref oldCursor, ref pendingOld, out var oldLine))
        {
            if (!TryRead(ref newCursor, ref pendingNew, out var newLine))
            {
                Tail(oldLine, ref oldNumber, oldCursor, viewMode, inserted: false, hasRows);
                return;
            }

            if (oldLine.SequenceEqual(newLine))
            {
                RowSeparator(hasRows);
                Unchanged(oldNumber++, newNumber++, oldLine);
                hasRows = true;
                continue;
            }

            Mismatch(oldCursor, newCursor, ref pendingOld, ref pendingNew, ref oldNumber, ref newNumber, oldLine, newLine, viewMode, hasRows);
            hasRows = true;
        }

        if (TryRead(ref newCursor, ref pendingNew, out var remainingNew))
        {
            Tail(remainingNew, ref newNumber, newCursor, viewMode, inserted: true, hasRows);
        }
    }

    private void Mismatch(TextLineCursor oldCursor, TextLineCursor newCursor, ref string? pendingOld, ref string? pendingNew, ref int oldNumber, ref int newNumber, scoped ReadOnlySpan<char> oldLine, scoped ReadOnlySpan<char> newLine, CommitDiffViewMode viewMode, bool hasRows)
    {
        if (oldCursor.Peek(out var nextOld) && nextOld.SequenceEqual(newLine))
        {
            pendingNew = newLine.ToString();
            RowSeparator(hasRows);
            Row(oldNumber++, null, oldLine, true, default, false, "Deleted", viewMode);
            return;
        }

        if (newCursor.Peek(out var nextNew) && oldLine.SequenceEqual(nextNew))
        {
            pendingOld = oldLine.ToString();
            RowSeparator(hasRows);
            Row(null, newNumber++, default, false, newLine, true, "Inserted", viewMode);
            return;
        }

        Modified(ref oldNumber, ref newNumber, oldLine, newLine, viewMode, hasRows);
    }

    private void Modified(ref int oldNumber, ref int newNumber, scoped ReadOnlySpan<char> oldLine, scoped ReadOnlySpan<char> newLine, CommitDiffViewMode viewMode, bool hasRows)
    {
        RowSeparator(hasRows);
        if (viewMode == CommitDiffViewMode.SideBySide)
        {
            Row(oldNumber++, newNumber++, oldLine, true, newLine, true, "Modified", viewMode);
            return;
        }

        Row(oldNumber++, null, oldLine, true, default, false, "Deleted", viewMode);
        Raw(',');
        Row(null, newNumber++, default, false, newLine, true, "Inserted", viewMode);
    }

    private void Tail(scoped ReadOnlySpan<char> first, ref int number, TextLineCursor cursor, CommitDiffViewMode viewMode, bool inserted, bool hasRows)
    {
        RowSeparator(hasRows);
        Row(inserted ? null : number, inserted ? number : null, first, !inserted, first, inserted, inserted ? "Inserted" : "Deleted", viewMode);
        number++;
        while (cursor.TryRead(out var line))
        {
            Raw(',');
            Row(inserted ? null : number, inserted ? number : null, line, !inserted, line, inserted, inserted ? "Inserted" : "Deleted", viewMode);
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
}
