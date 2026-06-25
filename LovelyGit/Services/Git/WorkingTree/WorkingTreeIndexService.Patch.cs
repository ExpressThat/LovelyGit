using System.Buffers;
using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

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
        return changeType switch
        {
            "Inserted" => BuildInsertedLinePatch(path, oldLineNumber, newLineNumber, newText),
            "Deleted" => BuildDeletedLinePatch(path, oldLineNumber, newLineNumber, oldText),
            "Modified" => BuildModifiedLinePatch(path, oldLineNumber, newLineNumber, oldText, newText),
            _ => throw new InvalidOperationException("Only changed lines can be staged."),
        };
    }

    private static string BuildInsertedLinePatch(
        string path,
        int? oldLineNumber,
        int? newLineNumber,
        string newText)
    {
        if (newLineNumber == null)
        {
            throw new InvalidOperationException("Inserted lines require a new line number.");
        }

        var oldStart = Math.Max(0, (oldLineNumber ?? newLineNumber.Value) - 1);
        return BuildPatch(path, $"@@ -{oldStart},0 +{newLineNumber.Value},1 @@", null, newText);
    }

    private static string BuildDeletedLinePatch(
        string path,
        int? oldLineNumber,
        int? newLineNumber,
        string oldText)
    {
        if (oldLineNumber == null)
        {
            throw new InvalidOperationException("Deleted lines require an old line number.");
        }

        var newStart = Math.Max(0, (newLineNumber ?? oldLineNumber.Value) - 1);
        return BuildPatch(path, $"@@ -{oldLineNumber.Value},1 +{newStart},0 @@", oldText, null);
    }

    private static string BuildModifiedLinePatch(
        string path,
        int? oldLineNumber,
        int? newLineNumber,
        string oldText,
        string newText)
    {
        if (oldLineNumber == null || newLineNumber == null)
        {
            throw new InvalidOperationException("Modified lines require old and new line numbers.");
        }

        return BuildPatch(path, $"@@ -{oldLineNumber.Value},1 +{newLineNumber.Value},1 @@", oldText, newText);
    }

    private static string BuildPatch(string path, string hunkHeader, string? oldText, string? newText)
    {
        var builder = new StringBuilder(path.Length + (oldText?.Length ?? 0) + (newText?.Length ?? 0) + 128);
        builder.Append("diff --git a/").Append(path).Append(" b/").Append(path).Append('\n');
        builder.Append("--- a/").Append(path).Append('\n');
        builder.Append("+++ b/").Append(path).Append('\n');
        builder.Append(hunkHeader).Append('\n');
        if (oldText != null)
        {
            builder.Append('-').Append(oldText).Append('\n');
        }

        if (newText != null)
        {
            builder.Append('+').Append(newText).Append('\n');
        }

        return builder.ToString();
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
