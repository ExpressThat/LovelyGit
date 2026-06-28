using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.OperationState;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace ExpressThat.LovelyGit.Services.Git.Conflicts;

internal sealed class GitConflictService
{
    private readonly GitOperationStateService _operationStateService;

    public GitConflictService(GitOperationStateService operationStateService)
    {
        _operationStateService = operationStateService;
    }

    public async Task<GitConflictStateResponse> GetStateAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var operation = await _operationStateService
            .GetStateAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        using var repository = await LovelyGitRepository
            .OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var indexEntries = await new GitIndexReader()
            .ReadAsync(repository.GitDirectory, repository.ObjectFormat, cancellationToken)
            .ConfigureAwait(false);
        var conflicted = await BuildConflictFilesAsync(
                repository.WorkTreeDirectory,
                indexEntries,
                cancellationToken)
            .ConfigureAwait(false);

        return new GitConflictStateResponse
        {
            Operation = operation,
            ConflictedFiles = conflicted,
            CommitMessage = await ReadCommitMessageAsync(repository.GitDirectory, cancellationToken)
                .ConfigureAwait(false),
        };
    }

    private static async Task<List<GitConflictFile>> BuildConflictFilesAsync(
        string workTreeDirectory,
        IReadOnlyList<GitIndexEntry> indexEntries,
        CancellationToken cancellationToken)
    {
        var result = new List<GitConflictFile>();
        var files = indexEntries
            .Where(entry => entry.Stage != 0)
            .GroupBy(entry => entry.Path, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal);

        foreach (var file in files)
        {
            var fullPath = Path.Combine(workTreeDirectory, FromGitPath(file.Key));
            var text = File.Exists(fullPath)
                ? await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false)
                : string.Empty;
            result.Add(new GitConflictFile
            {
                Path = file.Key,
                Status = "Unmerged",
                ConflictCount = CountConflictMarkers(text),
                IsBinary = false,
            });
        }

        return result;
    }

    private static async Task<string> ReadCommitMessageAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(gitDirectory, "MERGE_MSG");
        return File.Exists(path)
            ? await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false)
            : string.Empty;
    }

    private static int CountConflictMarkers(string text) =>
        text.Split('\n').Count(line => line.StartsWith("<<<<<<<", StringComparison.Ordinal));

    private static string FromGitPath(string path) =>
        path.Replace('/', Path.DirectorySeparatorChar);
}
