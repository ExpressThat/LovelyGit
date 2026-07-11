using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionService
{
    private static ConflictSourceMetadata CreateCurrentSource(
        LovelyGitRepository repository,
        GitIndexEntry? currentEntry) => new()
    {
        Label = "Current",
        RefName = repository.CurrentBranchName,
        ObjectId = repository.HeadTarget?.ToString() ?? currentEntry?.ObjectId.ToString(),
    };

    private static async Task<ConflictSourceMetadata> CreateIncomingSourceAsync(
        LovelyGitRepository repository,
        string worktreeGitDirectory,
        GitIndexEntry? incomingEntry,
        CancellationToken cancellationToken)
    {
        var target = await ReadOperationTargetAsync(
            worktreeGitDirectory,
            repository.ObjectFormat,
            cancellationToken).ConfigureAwait(false);
        var matchingRef = target == null
            ? null
            : repository.GetBranches()
                .OrderBy(reference => reference.Kind == GitRefKind.Head ? 0 : 1)
                .FirstOrDefault(reference => reference.Target == target.Value);
        return new ConflictSourceMetadata
        {
            Label = "Incoming",
            RefName = matchingRef?.Name,
            ObjectId = target?.ToString() ?? incomingEntry?.ObjectId.ToString(),
        };
    }

    private static async Task<GitObjectId?> ReadOperationTargetAsync(
        string worktreeGitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        foreach (var name in new[] { "MERGE_HEAD", "CHERRY_PICK_HEAD", "REVERT_HEAD", "REBASE_HEAD" })
        {
            var path = Path.Combine(worktreeGitDirectory, name);
            if (!File.Exists(path))
            {
                continue;
            }

            var text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            var firstLine = text.AsSpan().Trim();
            var newline = firstLine.IndexOfAny('\r', '\n');
            if (newline >= 0)
            {
                firstLine = firstLine[..newline];
            }

            if (GitObjectId.TryParse(firstLine, objectFormat, out var target))
            {
                return target;
            }
        }

        return null;
    }
}
