using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.Bisect;

internal sealed class GitBisectCommandService
{
    private readonly GitOperationService _operations;
    private readonly NativeGitBisectStateReader _reader;

    public GitBisectCommandService(
        GitOperationService operations,
        NativeGitBisectStateReader reader)
    {
        _operations = operations;
        _reader = reader;
    }

    public async Task<GitBisectState> ExecuteAsync(
        string repositoryPath,
        GitBisectAction action,
        string? goodCommit,
        CancellationToken cancellationToken)
    {
        var before = await _reader.ReadAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        if (action == GitBisectAction.Start && before.IsActive)
        {
            throw new InvalidOperationException("A bisect session is already active.");
        }

        if (action != GitBisectAction.Start && !before.IsActive)
        {
            throw new InvalidOperationException("No bisect session is active.");
        }

        var arguments = action == GitBisectAction.Start
            ? await BuildStartArgumentsAsync(repositoryPath, goodCommit, cancellationToken)
                .ConfigureAwait(false)
            : BuildActiveArguments(action);
        await _operations.ExecuteRequiredBufferedAsync(
                $"{action} bisect",
                arguments,
                repositoryPath,
                "Reset the bisect session if you need to restore the starting branch.",
                cancellationToken)
            .ConfigureAwait(false);
        return await _reader.ReadAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<string>> BuildStartArgumentsAsync(
        string repositoryPath,
        string? goodCommit,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var objectFormat = await GitRepositoryDiscovery
            .ReadObjectFormatAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        if (!GitObjectId.TryParse(goodCommit, objectFormat, out var goodId))
        {
            throw new ArgumentException("Select a valid known-good commit.", nameof(goodCommit));
        }
        using var objectStore = new GitObjectStore(paths.GitDirectory, objectFormat);
        if (!await CommitExistsAsync(objectStore, goodId, cancellationToken).ConfigureAwait(false))
        {
            throw new ArgumentException("Select a valid known-good commit.", nameof(goodCommit));
        }

        var badId = await GitHeadReader.ResolveAsync(
                paths.WorktreeGitDirectory,
                paths.GitDirectory,
                objectFormat,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("The repository does not have a HEAD commit.");
        if (badId == goodId)
        {
            throw new InvalidOperationException("The known-good commit must be earlier than HEAD.");
        }

        return ["bisect", "start", badId.ToString(), goodId.ToString()];
    }

    private static async Task<bool> CommitExistsAsync(
        GitObjectStore objectStore,
        GitObjectId id,
        CancellationToken cancellationToken)
    {
        try
        {
            var data = await objectStore.ReadObjectWithoutCachingAsync(id, cancellationToken)
                .ConfigureAwait(false);
            return data.Kind == GitObjectKind.Commit;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    private static IReadOnlyList<string> BuildActiveArguments(GitBisectAction action) =>
        action switch
        {
            GitBisectAction.MarkGood => ["bisect", "good"],
            GitBisectAction.MarkBad => ["bisect", "bad"],
            GitBisectAction.Skip => ["bisect", "skip"],
            GitBisectAction.Reset => ["bisect", "reset"],
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };
}
