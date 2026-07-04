using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using System.IO.Compression;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static class FastLineDiffBuilder
{
    private const int CompressVirtualTextCharacters = 256_000;

    public static CommitFileDiffResponse BuildVirtualTextBytes(
        string commitHash,
        string path,
        string status,
        CommitDiffViewMode viewMode,
        string changeType,
        byte[] bytes)
    {
        return new CommitFileDiffResponse
        {
            CommitHash = commitHash,
            Path = path,
            Status = status,
            ViewMode = viewMode,
            HasDifferences = true,
            VirtualTextGzipBase64 = CompressBytes(bytes),
            VirtualTextEncoding = "gzip-base64:utf-8",
            VirtualChangeType = changeType,
            VirtualLineCount = CountLines(bytes),
        };
    }
    public static CommitFileDiffResponse Build(
        string commitHash,
        string path,
        string status,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        string oldText,
        string newText)
    {
        if (oldText.Length == 0 && newText.Length > 0)
        {
            return BuildVirtualTextResponse(commitHash, path, status, viewMode, "Inserted", newText);
        }

        if (newText.Length == 0 && oldText.Length > 0)
        {
            return BuildVirtualTextResponse(commitHash, path, status, viewMode, "Deleted", oldText);
        }

        var oldLines = SplitLines(oldText);
        var newLines = SplitLines(newText);
        var lines = viewMode == CommitDiffViewMode.SideBySide
            ? BuildSideBySide(oldLines, newLines, ignoreWhitespace)
            : BuildCombined(oldLines, newLines, ignoreWhitespace);
        return new CommitFileDiffResponse
        {
            CommitHash = commitHash,
            Path = path,
            Status = status,
            ViewMode = viewMode,
            HasDifferences = !string.Equals(oldText, newText, StringComparison.Ordinal),
            Lines = lines,
        };
    }

    private static CommitFileDiffResponse BuildVirtualTextResponse(
        string commitHash,
        string path,
        string status,
        CommitDiffViewMode viewMode,
        string changeType,
        string text)
    {
        var shouldCompress = ShouldCompress(text);
        return new CommitFileDiffResponse
        {
            CommitHash = commitHash,
            Path = path,
            Status = status,
            ViewMode = viewMode,
            HasDifferences = true,
            VirtualText = shouldCompress ? null! : text,
            VirtualTextGzipBase64 = shouldCompress ? CompressText(text) : null!,
            VirtualTextEncoding = shouldCompress ? "gzip-base64:utf-8" : null!,
            VirtualChangeType = changeType,
            VirtualLineCount = CountLines(text),
        };
    }

    private static bool ShouldCompress(string text) => text.Length >= CompressVirtualTextCharacters;

    private static string CompressText(string text)
    {
        return CompressBytes(Encoding.UTF8.GetBytes(text));
    }

    private static string CompressBytes(ReadOnlySpan<byte> bytes)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(bytes);
        }

        return Convert.ToBase64String(output.GetBuffer(), 0, (int)output.Length);
    }

    private static List<CommitFileDiffLine> BuildSideBySide(string[] oldLines, string[] newLines, bool ignoreWhitespace)
    {
        var lines = new List<CommitFileDiffLine>(Math.Max(oldLines.Length, newLines.Length));
        Walk(oldLines, newLines, ignoreWhitespace, new SideBySideSink(lines));
        return lines;
    }

    private static List<CommitFileDiffLine> BuildCombined(string[] oldLines, string[] newLines, bool ignoreWhitespace)
    {
        var lines = new List<CommitFileDiffLine>(oldLines.Length + newLines.Length);
        Walk(oldLines, newLines, ignoreWhitespace, new CombinedSink(lines));
        return lines;
    }

    private static void Walk<TSink>(string[] oldLines, string[] newLines, bool ignoreWhitespace, TSink sink)
        where TSink : struct, IFastDiffSink
    {
        var oldIndex = 0;
        var newIndex = 0;
        while (oldIndex < oldLines.Length && newIndex < newLines.Length)
        {
            if (LineEquals(oldLines[oldIndex], newLines[newIndex], ignoreWhitespace))
            {
                sink.Unchanged(oldIndex + 1, newIndex + 1, oldLines[oldIndex]);
                oldIndex++;
                newIndex++;
            }
            else if (oldIndex + 1 < oldLines.Length && LineEquals(oldLines[oldIndex + 1], newLines[newIndex], ignoreWhitespace))
            {
                sink.Deleted(oldIndex + 1, oldLines[oldIndex]);
                oldIndex++;
            }
            else if (newIndex + 1 < newLines.Length && LineEquals(oldLines[oldIndex], newLines[newIndex + 1], ignoreWhitespace))
            {
                sink.Inserted(newIndex + 1, newLines[newIndex]);
                newIndex++;
            }
            else
            {
                sink.Modified(oldIndex + 1, newIndex + 1, oldLines[oldIndex], newLines[newIndex]);
                oldIndex++;
                newIndex++;
            }
        }

        while (oldIndex < oldLines.Length)
        {
            sink.Deleted(oldIndex + 1, oldLines[oldIndex++]);
        }

        while (newIndex < newLines.Length)
        {
            sink.Inserted(newIndex + 1, newLines[newIndex++]);
        }
    }

    private static string[] SplitLines(string text)
    {
        if (text.Length == 0)
        {
            return [];
        }

        return text.ReplaceLineEndings("\n").Split('\n');
    }

    private static int CountLines(string text)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        var count = 1;
        foreach (var character in text)
        {
            if (character == '\n')
            {
                count++;
            }
        }

        return count;
    }

    private static int CountLines(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return 0;
        }

        var count = 1;
        foreach (var value in bytes)
        {
            if (value == (byte)'\n')
            {
                count++;
            }
        }

        return count;
    }

    private static bool LineEquals(string left, string right, bool ignoreWhitespace)
    {
        return ignoreWhitespace
            ? WhitespaceInsensitiveEquals(left, right)
            : string.Equals(left, right, StringComparison.Ordinal);
    }

    private static bool WhitespaceInsensitiveEquals(string left, string right)
    {
        var leftIndex = 0;
        var rightIndex = 0;
        while (true)
        {
            while (leftIndex < left.Length && char.IsWhiteSpace(left[leftIndex])) leftIndex++;
            while (rightIndex < right.Length && char.IsWhiteSpace(right[rightIndex])) rightIndex++;
            if (leftIndex == left.Length || rightIndex == right.Length)
            {
                return leftIndex == left.Length && rightIndex == right.Length;
            }

            if (left[leftIndex++] != right[rightIndex++])
            {
                return false;
            }
        }
    }
}
