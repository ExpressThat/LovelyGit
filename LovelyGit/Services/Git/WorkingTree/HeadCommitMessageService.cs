using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class HeadCommitMessageService
{
    public async Task<HeadCommitMessageResponse> GetAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var objectFormat = await GitRepositoryDiscovery
            .ReadObjectFormatAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var headTarget = await GitHeadReader
            .ResolveAsync(
                paths.WorktreeGitDirectory,
                paths.GitDirectory,
                objectFormat,
                cancellationToken)
            .ConfigureAwait(false);
        if (headTarget is not { } commitId)
        {
            throw new InvalidOperationException("The repository does not have a commit to amend.");
        }

        using var objectStore = new GitObjectStore(paths.GitDirectory, objectFormat);
        var data = await objectStore
            .ReadObjectAsync(commitId, cacheObject: false, cancellationToken)
            .ConfigureAwait(false);
        if (data.Kind != GitObjectKind.Commit)
        {
            throw new InvalidDataException($"HEAD is not a commit: {commitId}");
        }

        var commit = GitObjectParsers.ParseCommit(commitId, data.Data);
        return new HeadCommitMessageResponse
        {
            Hash = commitId.ToString(),
            Title = commit.Subject,
            Body = ExtractDescription(commit.Body),
        };
    }

    private static string ExtractDescription(string message)
    {
        var value = message.AsSpan();
        var end = value.Length;
        while (end > 0 && value[end - 1] is '\r' or '\n')
        {
            end--;
        }

        var titleEnd = value[..end].IndexOf('\n');
        if (titleEnd < 0)
        {
            return string.Empty;
        }

        var bodyStart = titleEnd + 1;
        while (bodyStart < end && value[bodyStart] is '\r' or '\n')
        {
            bodyStart++;
        }

        return bodyStart < end ? value[bodyStart..end].ToString() : string.Empty;
    }
}
