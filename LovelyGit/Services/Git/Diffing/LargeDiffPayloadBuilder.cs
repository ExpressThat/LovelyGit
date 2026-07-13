using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using System.IO.Compression;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static class LargeDiffPayloadBuilder
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

        var model = LineDiffEngine.Build(oldText, newText, ignoreWhitespace);
        var lines = viewMode == CommitDiffViewMode.SideBySide
            ? BuildSideBySide(model)
            : BuildCombined(model);
        return new CommitFileDiffResponse
        {
            CommitHash = commitHash,
            Path = path,
            Status = status,
            ViewMode = viewMode,
            HasDifferences = model.HasDifferences,
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

    private static List<CommitFileDiffLine> BuildSideBySide(LineDiffModel model)
    {
        var lines = new List<CommitFileDiffLine>(model.Rows.Count);
        Walk(model, new SideBySideSink(lines));
        return lines;
    }

    private static List<CommitFileDiffLine> BuildCombined(LineDiffModel model)
    {
        var lines = new List<CommitFileDiffLine>(model.Rows.Count * 2);
        Walk(model, new CombinedSink(lines));
        return lines;
    }

    private static void Walk<TSink>(LineDiffModel model, TSink sink)
        where TSink : struct, IFastDiffSink
    {
        foreach (var row in model.Rows)
        {
            var oldText = row.OldIndex is int oldIndex ? model.OldLines[oldIndex] : string.Empty;
            var newText = row.NewIndex is int newIndex ? model.NewLines[newIndex] : string.Empty;
            if (!row.IsChanged) sink.Unchanged(row.OldIndex!.Value + 1, row.NewIndex!.Value + 1, oldText);
            else if (row.OldIndex is null) sink.Inserted(row.NewIndex!.Value + 1, newText);
            else if (row.NewIndex is null) sink.Deleted(row.OldIndex.Value + 1, oldText);
            else sink.Modified(row.OldIndex.Value + 1, row.NewIndex.Value + 1, oldText, newText);
        }
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

}
