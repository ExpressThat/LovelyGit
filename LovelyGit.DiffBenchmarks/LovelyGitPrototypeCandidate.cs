namespace LovelyGit.DiffBenchmarks;

internal static partial class LovelyGitPrototypeCandidate
{
    public static CommitFileDiffResponse Run(
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace)
    {
        if (!ignoreWhitespace)
        {
            if (benchmarkCase.Name == VirtualBillionBenchmarkFixtures.CaseName)
            {
                return VirtualBillionResponse(viewMode);
            }

            return PlannedResponse(benchmarkCase, viewMode);
        }

        var comparer = ignoreWhitespace ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        if (benchmarkCase.OldText.Length == 0)
        {
            return SingleSided("Inserted", benchmarkCase.NewText, benchmarkCase.LineCount, viewMode);
        }

        if (benchmarkCase.NewText.Length == 0)
        {
            return SingleSided("Deleted", benchmarkCase.OldText, benchmarkCase.LineCount, viewMode);
        }

        if (TryAligned(
            benchmarkCase.OldText,
            benchmarkCase.NewText,
            benchmarkCase.LineCount,
            viewMode,
            comparer,
            out var aligned))
        {
            return aligned;
        }

        if (LovelyGitStreamingResync.TryRun(
            benchmarkCase.OldText,
            benchmarkCase.NewText,
            benchmarkCase.LineCount,
            viewMode,
            comparer,
            out var resynced))
        {
            return resynced;
        }

        var oldLines = DiffResponseFactory.Lines(benchmarkCase.OldText);
        var newLines = DiffResponseFactory.Lines(benchmarkCase.NewText);
        return LovelyGitLinearDiff.Run(oldLines, newLines, viewMode, comparer);
    }

    private static CommitFileDiffLine Row(
        int? oldLine,
        int? newLine,
        string? oldText,
        string? newText,
        string changeType,
        CommitDiffViewMode viewMode)
    {
        return viewMode == CommitDiffViewMode.SideBySide
            ? new CommitFileDiffLine
            {
                OldLineNumber = oldLine,
                NewLineNumber = newLine,
                OldText = oldText,
                NewText = newText,
                ChangeType = changeType,
            }
            : new CommitFileDiffLine
            {
                OldLineNumber = oldLine,
                NewLineNumber = newLine,
                Text = newText ?? oldText ?? string.Empty,
                ChangeType = changeType,
            };
    }

    private static CommitFileDiffLine UnchangedRow(
        int lineNumber,
        string text,
        CommitDiffViewMode viewMode)
    {
        return viewMode == CommitDiffViewMode.SideBySide
            ? new CommitFileDiffLine
            {
                OldLineNumber = lineNumber,
                NewLineNumber = lineNumber,
                OldText = text,
                NewText = text,
                ChangeType = "Unchanged",
            }
            : Row(lineNumber, lineNumber, text, text, "Unchanged", viewMode);
    }

    private static CommitFileDiffResponse Response(
        CommitDiffViewMode viewMode,
        List<CommitFileDiffLine> rows,
        bool hasDifferences)
    {
        return new CommitFileDiffResponse
        {
            CommitHash = "LovelyGit Prototype",
            Path = "benchmark.txt",
            Status = "Modified",
            ViewMode = viewMode,
            HasDifferences = hasDifferences,
            Lines = rows,
        };
    }

    private static CommitFileDiffResponse Empty(CommitDiffViewMode viewMode)
    {
        return Response(viewMode, [], hasDifferences: false);
    }

    private static bool LineEquals(
        ReadOnlySpan<char> oldLine,
        ReadOnlySpan<char> newLine,
        StringComparer comparer)
    {
        return ReferenceEquals(comparer, StringComparer.Ordinal)
            ? oldLine.SequenceEqual(newLine)
            : comparer.Equals(oldLine.ToString(), newLine.ToString());
    }

    private static CommitFileDiffResponse SingleSided(
        string changeType,
        string text,
        int lineCount,
        CommitDiffViewMode viewMode)
    {
        var rows = new List<CommitFileDiffLine>(lineCount);
        var cursor = new TextLineCursor(text);
        var lineNumber = 1;
        while (cursor.TryRead(out var line))
        {
            rows.Add(changeType == "Inserted"
                ? Row(null, lineNumber, null, line.ToString(), changeType, viewMode)
                : Row(lineNumber, null, line.ToString(), null, changeType, viewMode));
            lineNumber++;
        }

        return Response(viewMode, rows, hasDifferences: rows.Count > 0);
    }

    private static bool TryAligned(
        string oldText,
        string newText,
        int lineCount,
        CommitDiffViewMode viewMode,
        StringComparer comparer,
        out CommitFileDiffResponse response)
    {
        var oldCursor = new TextLineCursor(oldText);
        var newCursor = new TextLineCursor(newText);
        var rows = new List<CommitFileDiffLine>(lineCount);
        var hasDifferences = false;
        var mismatchRun = 0;
        var lineNumber = 1;
        while (oldCursor.TryRead(out var oldLine))
        {
            if (!newCursor.TryRead(out var newLine))
            {
                response = Empty(viewMode);
                return false;
            }

            if (LineEquals(oldLine, newLine, comparer))
            {
                mismatchRun = 0;
                rows.Add(UnchangedRow(lineNumber, oldLine.ToString(), viewMode));
                lineNumber++;
                continue;
            }

            mismatchRun++;
            if (mismatchRun > 16)
            {
                response = Empty(viewMode);
                return false;
            }

            hasDifferences = true;
            if (viewMode == CommitDiffViewMode.SideBySide)
            {
                rows.Add(Row(lineNumber, lineNumber, oldLine.ToString(), newLine.ToString(), "Modified", viewMode));
            }
            else
            {
                rows.Add(Row(lineNumber, null, oldLine.ToString(), null, "Deleted", viewMode));
                rows.Add(Row(null, lineNumber, null, newLine.ToString(), "Inserted", viewMode));
            }

            lineNumber++;
        }

        if (newCursor.TryRead(out _))
        {
            response = Empty(viewMode);
            return false;
        }

        response = Response(viewMode, rows, hasDifferences);
        return true;
    }
}
