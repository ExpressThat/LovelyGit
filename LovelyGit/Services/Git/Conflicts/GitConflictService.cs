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
        if (operation.Kind == GitOperationKind.None)
        {
            return new GitConflictStateResponse
            {
                Operation = operation,
            };
        }

        using var repository = await LovelyGitRepository
            .OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var labels = await GitConflictSideLabelService.BuildAsync(repository, operation, cancellationToken)
            .ConfigureAwait(false);
        var indexEntries = await new GitIndexReader()
            .ReadAsync(repository.GitDirectory, repository.ObjectFormat, cancellationToken)
            .ConfigureAwait(false);
        var conflicted = await BuildConflictFilesAsync(
                repository.WorkTreeDirectory,
                indexEntries,
                cancellationToken)
            .ConfigureAwait(false);
        var commitMessage = await ReadCommitMessageAsync(repository.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var conflictedPaths = conflicted.Select(file => file.Path).ToHashSet(StringComparer.Ordinal);

        return new GitConflictStateResponse
        {
            Operation = operation,
            OursLabel = labels.Ours,
            TheirsLabel = labels.Theirs,
            ConflictedFiles = conflicted,
            ResolvedFiles = BuildResolvedFiles(commitMessage, conflictedPaths),
            CommitMessage = commitMessage,
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

    private static List<GitConflictFile> BuildResolvedFiles(
        string commitMessage,
        HashSet<string> conflictedPaths)
    {
        return ExtractConflictPaths(commitMessage)
            .Where(path => !conflictedPaths.Contains(path))
            .Select(path => new GitConflictFile
            {
                Path = path,
                Status = "Resolved",
            })
            .ToList();
    }

    private static async Task<string> ReadCommitMessageAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        foreach (var path in EnumerateMessagePaths(gitDirectory))
        {
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            }
        }

        return string.Empty;
    }

    private static IEnumerable<string> EnumerateMessagePaths(string gitDirectory)
    {
        yield return Path.Combine(gitDirectory, "MERGE_MSG");
        yield return Path.Combine(gitDirectory, "rebase-merge", "message");
        yield return Path.Combine(gitDirectory, "rebase-apply", "final-commit");
    }

    internal static IEnumerable<string> ExtractConflictPaths(string commitMessage)
    {
        var inConflictBlock = false;
        foreach (var line in commitMessage.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Equals("# Conflicts:", StringComparison.Ordinal)
                || trimmed.Equals("Conflicts:", StringComparison.Ordinal))
            {
                inConflictBlock = true;
                continue;
            }

            if (!inConflictBlock)
            {
                continue;
            }

            trimmed = trimmed.TrimStart('#').Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.Contains(':', StringComparison.Ordinal))
            {
                yield break;
            }

            yield return trimmed.Replace('\\', '/');
        }
    }

    private static int CountConflictMarkers(string text) =>
        text.Split('\n').Count(line => line.StartsWith("<<<<<<<", StringComparison.Ordinal));

    private static string FromGitPath(string path) =>
        path.Replace('/', Path.DirectorySeparatorChar);

}
