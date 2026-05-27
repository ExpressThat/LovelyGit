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

        var parts = cursor.Split(':', 2);
        if (parts.Length == 2 && Guid.TryParse(parts[0], out var repositoryId) && int.TryParse(parts[1], out var offset))
        {
            return new CommitGraphCursorState(repositoryId, offset);
        }

        return Guid.TryParse(cursor, out repositoryId)
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
