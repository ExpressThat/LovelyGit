using System.Security.Cryptography;
using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ExpressThat.LovelyGit.Services.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeChangeService
{
    private static IEnumerable<string> SafeEnumerateDirectories(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static GitObjectId ComputeBlobObjectId(byte[] bytes, GitObjectFormat objectFormat)
    {
        var header = System.Text.Encoding.ASCII.GetBytes($"blob {bytes.Length}\0");
        var combined = new byte[header.Length + bytes.Length];
        Buffer.BlockCopy(header, 0, combined, 0, header.Length);
        Buffer.BlockCopy(bytes, 0, combined, header.Length, bytes.Length);
        var hash = objectFormat == GitObjectFormat.Sha256 ? SHA256.HashData(combined) : SHA1.HashData(combined);
        return new GitObjectId(Convert.ToHexString(hash).ToLowerInvariant(), objectFormat);
    }

    private static async Task<byte[]?> TryReadBlobBytesAsync(
        LovelyGitRepository repository,
        GitObjectId objectId,
        string mode,
        CancellationToken cancellationToken)
    {
        if (IsSubmoduleMode(mode))
        {
            return null;
        }

        try
        {
            return await repository.ReadBlobAsync(objectId, cancellationToken).ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static (uint Additions, uint Deletions, bool IsBinary) CalculateStats(byte[]? oldBytes, byte[]? newBytes)
    {
        if (oldBytes == null || newBytes == null)
        {
            return (0, 0, true);
        }

        var oldBinary = IsBinary(oldBytes);
        var newBinary = IsBinary(newBytes);
        if (oldBinary || newBinary)
        {
            return (0, 0, true);
        }

        var oldLines = SplitLines(oldBytes);
        var newLines = SplitLines(newBytes);
        var common = CountCommonLines(oldLines, newLines);
        return ((uint)(newLines.Length - common), (uint)(oldLines.Length - common), false);
    }

    private static int CountCommonLines(string[] oldLines, string[] newLines)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var line in oldLines)
        {
            counts.TryGetValue(line, out var count);
            counts[line] = count + 1;
        }

        var common = 0;
        foreach (var line in newLines)
        {
            if (!counts.TryGetValue(line, out var count) || count == 0)
            {
                continue;
            }

            common++;
            if (count == 1)
            {
                counts.Remove(line);
            }
            else
            {
                counts[line] = count - 1;
            }
        }

        return common;
    }

    internal static CommitFileDiffResponse BuildDiffResponse(
        string commitHash,
        string path,
        string status,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        byte[] oldBytes,
        byte[] newBytes,
        bool compact = true)
    {
        using var trace = LovelyGitTrace.Time(
            "working-tree.build-diff-response",
            $"{path} old={oldBytes.Length} new={newBytes.Length}");
        var isBinary = IsBinary(oldBytes) || IsBinary(newBytes);
        if (isBinary)
        {
            return new CommitFileDiffResponse
            {
                CommitHash = commitHash,
                Path = path,
                Status = status,
                ViewMode = viewMode,
                IsBinary = true,
                HasDifferences = true,
            };
        }

        if (oldBytes.Length == 0 && ShouldUseCompressedVirtualBytes(newBytes))
        {
            return FastLineDiffBuilder.BuildVirtualTextBytes(
                commitHash,
                path,
                status,
                viewMode,
                "Inserted",
                newBytes);
        }

        if (newBytes.Length == 0 && ShouldUseCompressedVirtualBytes(oldBytes))
        {
            return FastLineDiffBuilder.BuildVirtualTextBytes(
                commitHash,
                path,
                status,
                viewMode,
                "Deleted",
                oldBytes);
        }

        var oldText = System.Text.Encoding.UTF8.GetString(oldBytes);
        var newText = System.Text.Encoding.UTF8.GetString(newBytes);
        if (DiffInputGuard.ShouldUseFastDiff(oldText, newText))
        {
            var fastResponse = FastLineDiffBuilder.Build(
                commitHash,
                path,
                status,
                viewMode,
                ignoreWhitespace,
                oldText,
                newText);
            return compact ? CompactDiffPayloadBuilder.CompactIfUseful(fastResponse) : fastResponse;
        }

        var language = oldText.Length + newText.Length <= MaxSyntaxHighlightedCharacters
            ? ResolveLanguage(path)
            : null;

        var response = viewMode == CommitDiffViewMode.SideBySide
            ? BuildSideBySideResponse(commitHash, path, status, oldText, newText, language, ignoreWhitespace)
            : BuildCombinedResponse(commitHash, path, status, oldText, newText, language, ignoreWhitespace);
        return compact ? CompactDiffPayloadBuilder.CompactIfUseful(response) : response;
    }

    private static bool ShouldUseCompressedVirtualBytes(byte[] bytes) => bytes.Length >= 256_000;

}
