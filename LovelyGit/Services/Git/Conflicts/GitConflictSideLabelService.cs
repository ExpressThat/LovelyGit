using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.OperationState;

namespace ExpressThat.LovelyGit.Services.Git.Conflicts;

internal static class GitConflictSideLabelService
{
    public static async Task<GitConflictSideLabels> BuildAsync(
        LovelyGitRepository repository,
        GitOperationState operation,
        CancellationToken cancellationToken)
    {
        var currentBranch = repository.CurrentBranchName ?? "detached HEAD";
        return operation.Kind switch
        {
            GitOperationKind.Merge => new(
                $"Current: {currentBranch}",
                $"Incoming: {await ResolveMergeHeadLabelAsync(repository, cancellationToken).ConfigureAwait(false)}"),
            GitOperationKind.Rebase => new(
                $"Onto: {await ReadRebaseLabelAsync(repository.GitDirectory, "onto_name", "onto", cancellationToken)
                    .ConfigureAwait(false)}",
                $"Replaying: {await ReadRebaseLabelAsync(repository.GitDirectory, "head-name", null, cancellationToken)
                    .ConfigureAwait(false)}"),
            GitOperationKind.CherryPick => new($"Current: {currentBranch}", "Cherry-picked commit"),
            GitOperationKind.Revert => new($"Current: {currentBranch}", "Reverted commit"),
            _ => new("Current version", "Incoming version"),
        };
    }

    private static async Task<string> ResolveMergeHeadLabelAsync(
        LovelyGitRepository repository,
        CancellationToken cancellationToken)
    {
        var mergeHeadPath = Path.Combine(repository.GitDirectory, "MERGE_HEAD");
        if (!File.Exists(mergeHeadPath))
        {
            return "incoming commit";
        }

        var text = (await File.ReadAllTextAsync(mergeHeadPath, cancellationToken).ConfigureAwait(false))
            .Trim();
        if (!GitObjectId.TryParse(text, repository.ObjectFormat, out var mergeHead))
        {
            return "incoming commit";
        }

        return repository.GetBranches()
                   .OrderBy(reference => reference.Kind)
                   .ThenBy(reference => reference.Name, StringComparer.Ordinal)
                   .FirstOrDefault(reference => reference.Target == mergeHead)?.Name
               ?? ShortHash(text);
    }

    private static async Task<string> ReadRebaseLabelAsync(
        string gitDirectory,
        string preferredFile,
        string? fallbackFile,
        CancellationToken cancellationToken)
    {
        foreach (var rebaseDirectory in EnumerateRebaseDirectories(gitDirectory))
        {
            var label = await ReadRebaseFileAsync(rebaseDirectory, preferredFile, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(label))
            {
                return CleanRefName(label);
            }

            if (fallbackFile == null)
            {
                continue;
            }

            label = await ReadRebaseFileAsync(rebaseDirectory, fallbackFile, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(label))
            {
                return fallbackFile == "onto" ? ShortHash(label) : CleanRefName(label);
            }
        }

        return "unknown";
    }

    private static async Task<string> ReadRebaseFileAsync(
        string rebaseDirectory,
        string fileName,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(rebaseDirectory, fileName);
        return File.Exists(path)
            ? (await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false)).Trim()
            : string.Empty;
    }

    private static IEnumerable<string> EnumerateRebaseDirectories(string gitDirectory)
    {
        yield return Path.Combine(gitDirectory, "rebase-merge");
        yield return Path.Combine(gitDirectory, "rebase-apply");
    }

    private static string CleanRefName(string name)
    {
        const string headPrefix = "refs/heads/";
        const string remotePrefix = "refs/remotes/";
        if (name.StartsWith(headPrefix, StringComparison.Ordinal))
        {
            return name[headPrefix.Length..];
        }

        return name.StartsWith(remotePrefix, StringComparison.Ordinal)
            ? name[remotePrefix.Length..]
            : name;
    }

    private static string ShortHash(string hash) =>
        hash.Length > 7 ? hash[..7] : hash;
}

internal sealed record GitConflictSideLabels(string Ours, string Theirs);
