using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class CommitGraphCursor
{
    public static CommitGraphCursorState Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return new CommitGraphCursorState(null, 0);
        }

        var cursorSpan = cursor.AsSpan().Trim();
        var separator = cursorSpan.IndexOf(':');
        if (separator > 0
            && Guid.TryParse(cursorSpan[..separator], out var repositoryId)
            && int.TryParse(cursorSpan[(separator + 1)..], out var offset))
        {
            return new CommitGraphCursorState(repositoryId, offset);
        }

        return Guid.TryParse(cursorSpan, out repositoryId)
            ? new CommitGraphCursorState(repositoryId, 0)
            : new CommitGraphCursorState(null, 0);
    }

    public static string Encode(CommitGraphCursorState cursor)
    {
        return cursor.RepositoryId == null ? string.Empty : $"{cursor.RepositoryId}:{cursor.Offset}";
    }
}

public readonly record struct CommitGraphOpenResult(bool Success, CommitGraphManager? Graph, string? Error);
public readonly record struct CommitGraphCursorState(Guid? RepositoryId, int Offset);
public readonly record struct CommitGraphPageResult(CommitGraphResponse Response, CommitGraphCursorState NextCursor);
