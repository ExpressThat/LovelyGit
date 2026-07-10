using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

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
        using var repository = await LovelyGitRepository
            .OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        if (!GitObjectId.TryParse(goodCommit, repository.ObjectFormat, out var goodId) ||
            !await CommitExistsAsync(repository, goodId, cancellationToken).ConfigureAwait(false))
        {
            throw new ArgumentException("Select a valid known-good commit.", nameof(goodCommit));
        }

        var badId = repository.HeadTarget
            ?? throw new InvalidOperationException("The repository does not have a HEAD commit.");
        if (badId == goodId)
        {
            throw new InvalidOperationException("The known-good commit must be earlier than HEAD.");
        }

        return ["bisect", "start", badId.ToString(), goodId.ToString()];
    }

    private static async Task<bool> CommitExistsAsync(
        LovelyGitRepository repository,
        GitObjectId id,
        CancellationToken cancellationToken)
    {
        try
        {
            return await repository.GetCommitAsync(id, cancellationToken).ConfigureAwait(false) != null;
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
