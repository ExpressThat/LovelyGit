namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository
{
    public static async Task<LovelyGitRepository> OpenObjectDatabaseAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(path, cancellationToken)
            .ConfigureAwait(false);
        var objectFormat = await GitRepositoryDiscovery
            .ReadObjectFormatAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);

        return new LovelyGitRepository(
            paths.GitDirectory,
            paths.WorktreeGitDirectory,
            paths.WorkTreeDirectory,
            objectFormat,
            new GitObjectStore(paths.GitDirectory, objectFormat),
            headTarget: null,
            currentBranchName: null,
            new Dictionary<string, GitRef>(StringComparer.Ordinal),
            new Dictionary<GitObjectId, List<GitCommitRef>>(),
            Array.Empty<string>());
    }
}
