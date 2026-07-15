using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionService
{
    private static ConflictSourceMetadata CreateCurrentSource(
        GitHeadState head,
        GitIndexEntry? currentEntry) => new()
    {
        Label = "Current",
        RefName = head.BranchName,
        ObjectId = head.Target?.ToString() ?? currentEntry?.ObjectId.ToString(),
    };

    private static async Task<ConflictSourceMetadata> CreateIncomingSourceAsync(
        string gitDirectory,
        string worktreeGitDirectory,
        GitObjectFormat objectFormat,
        GitIndexEntry? incomingEntry,
        CancellationToken cancellationToken)
    {
        var target = await ReadOperationTargetAsync(
            worktreeGitDirectory,
            objectFormat,
            cancellationToken).ConfigureAwait(false);
        var matchingRef = target == null
            ? null
            : await GitRefTargetNameReader.FindBranchNameAsync(
                gitDirectory, objectFormat, target.Value, cancellationToken).ConfigureAwait(false);
        return new ConflictSourceMetadata
        {
            Label = "Incoming",
            RefName = matchingRef,
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
