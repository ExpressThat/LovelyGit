using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class CommitFileDiffCachingPolicy
{
    internal const int MaximumPersistentPayloadCharacters = 128 * 1024;

    public static bool CanUsePersistentCache(int parentIndex, bool ignoreWhitespace)
    {
        _ = ignoreWhitespace;
        return parentIndex == 0;
    }

    public static bool ShouldPersist(CommitFileDiffResponse response)
    {
        var remaining = MaximumPersistentPayloadCharacters;
        if (!Consume(response.CompactLinesGzipBase64, ref remaining)
            || !Consume(response.VirtualTextGzipBase64, ref remaining)
            || !Consume(response.VirtualText, ref remaining))
        {
            return false;
        }

        foreach (var line in response.Lines)
        {
            remaining -= 64;
            if (remaining < 0
                || !Consume(line.OldText, ref remaining)
                || !Consume(line.NewText, ref remaining)
                || !Consume(line.Text, ref remaining))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Consume(string? value, ref int remaining)
    {
        remaining -= value?.Length ?? 0;
        return remaining >= 0;
    }
}
