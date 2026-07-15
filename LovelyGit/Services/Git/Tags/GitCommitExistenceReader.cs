using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Tags;

internal static class GitCommitExistenceReader
{
    public static async Task EnsureExistsAsync(
        string repositoryPath,
        string commitHash,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var objectFormat = await GitRepositoryDiscovery
            .ReadObjectFormatAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        if (!GitObjectId.TryParse(commitHash, objectFormat, out var id))
        {
            throw new ArgumentException(
                "Commit hash does not match this repository.", nameof(commitHash));
        }

        using var objectStore = new GitObjectStore(paths.GitDirectory, objectFormat);
        try
        {
            var data = await objectStore
                .ReadObjectWithoutCachingAsync(id, cancellationToken)
                .ConfigureAwait(false);
            if (data.Kind == GitObjectKind.Commit)
            {
                return;
            }

            throw new InvalidDataException($"Object is not a commit: {id}");
        }
        catch (FileNotFoundException)
        {
        }

        throw new ArgumentException("Target commit does not exist.", nameof(commitHash));
    }
}
