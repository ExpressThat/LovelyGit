using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService
{
    internal static bool IsValidCachedDiff(CommitFileDiffResponse response)
    {
        if (response.IsBinary || !response.HasDifferences)
        {
            return true;
        }

        return response.Lines.Count > 0
            || response.VirtualLineCount > 0
            || response.CompactLineCount > 0
            || !string.IsNullOrEmpty(response.VirtualText)
            || !string.IsNullOrEmpty(response.VirtualTextGzipBase64)
            || !string.IsNullOrEmpty(response.CompactLinesGzipBase64);
    }
}
