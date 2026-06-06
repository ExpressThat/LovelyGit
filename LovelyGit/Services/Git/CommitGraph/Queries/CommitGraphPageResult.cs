using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;

internal sealed record CommitGraphPageQueryResult(
    bool IsSuccess,
    CommitGraphResponse? Response,
    string? ErrorMessage)
{
    public static CommitGraphPageQueryResult Success(CommitGraphResponse response)
    {
        return new CommitGraphPageQueryResult(true, response, null);
    }

    public static CommitGraphPageQueryResult Failure(string errorMessage)
    {
        return new CommitGraphPageQueryResult(false, null, errorMessage);
    }
}
