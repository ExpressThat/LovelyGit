using System.Buffers;
using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeIndexService
{
    private static string BuildSingleLinePatch(
        string path,
        string changeType,
        int? oldLineNumber,
        int? newLineNumber,
        string oldText,
        string newText)
    {
        return BuildHunkPatch(
            path,
            [new WorkingTreePatchLine
            {
                ChangeType = changeType,
                OldLineNumber = oldLineNumber,
                NewLineNumber = newLineNumber,
                OldText = oldText,
                NewText = newText,
            }]);
    }

    private static string BuildHunkPatch(
        string path,
        IReadOnlyList<WorkingTreePatchLine> lines)
    {
        var estimatedLength = path.Length + 96;
        foreach (var line in lines)
        {
            estimatedLength += line.OldText.Length + line.NewText.Length + 48;
        }

        var builder = new StringBuilder(estimatedLength);
        builder.Append("diff --git a/").Append(path).Append(" b/").Append(path).Append('\n');
        builder.Append("--- a/").Append(path).Append('\n');
        builder.Append("+++ b/").Append(path).Append('\n');
        foreach (var line in lines)
        {
            AppendLineHunk(builder, line);
        }

        return builder.ToString();
    }

    private static void AppendLineHunk(StringBuilder builder, WorkingTreePatchLine line)
    {
        switch (line.ChangeType)
        {
            case "Inserted":
                AppendInsertedLineHunk(builder, line.OldLineNumber, line.NewLineNumber, line.NewText);
                break;
            case "Deleted":
                AppendDeletedLineHunk(builder, line.OldLineNumber, line.NewLineNumber, line.OldText);
                break;
            case "Modified":
                AppendModifiedLineHunk(
                    builder,
                    line.OldLineNumber,
                    line.NewLineNumber,
                    line.OldText,
                    line.NewText);
                break;
            default:
                throw new InvalidOperationException("Only changed lines can be included in a hunk.");
        }
    }

    private static void AppendInsertedLineHunk(
        StringBuilder builder,
        int? oldLineNumber,
        int? newLineNumber,
        string newText)
    {
        if (newLineNumber == null)
        {
            throw new InvalidOperationException("Inserted lines require a new line number.");
        }

        var oldStart = Math.Max(0, (oldLineNumber ?? newLineNumber.Value) - 1);
        AppendPatchHunk(builder, $"@@ -{oldStart},0 +{newLineNumber.Value},1 @@", null, newText);
    }

    private static void AppendDeletedLineHunk(
        StringBuilder builder,
        int? oldLineNumber,
        int? newLineNumber,
        string oldText)
    {
        if (oldLineNumber == null)
        {
            throw new InvalidOperationException("Deleted lines require an old line number.");
        }

        var newStart = Math.Max(0, (newLineNumber ?? oldLineNumber.Value) - 1);
        AppendPatchHunk(builder, $"@@ -{oldLineNumber.Value},1 +{newStart},0 @@", oldText, null);
    }

    private static void AppendModifiedLineHunk(
        StringBuilder builder,
        int? oldLineNumber,
        int? newLineNumber,
        string oldText,
        string newText)
    {
        if (oldLineNumber == null || newLineNumber == null)
        {
            throw new InvalidOperationException("Modified lines require old and new line numbers.");
        }

        AppendPatchHunk(
            builder,
            $"@@ -{oldLineNumber.Value},1 +{newLineNumber.Value},1 @@",
            oldText,
            newText);
    }

    private static void AppendPatchHunk(
        StringBuilder builder,
        string hunkHeader,
        string? oldText,
        string? newText)
    {
        builder.Append(hunkHeader).Append('\n');
        if (oldText != null)
        {
            builder.Append('-').Append(oldText).Append('\n');
        }

        if (newText != null)
        {
            builder.Append('+').Append(newText).Append('\n');
        }
    }

    private static string? FirstNonEmptyLine(string text)
    {
        foreach (var line in text.AsSpan().EnumerateLines())
        {
            var trimmed = line.Trim();
            if (!trimmed.IsEmpty)
            {
                return trimmed.ToString();
            }
        }

        return null;
    }
}
