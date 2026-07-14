using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeStatusListService
{
    private const string Sha1EmptyTree = "4b825dc642cb6eb9a060e54bf8d69288fbee4904";
    private const string Sha256EmptyTree = "6ef19b41225c5369f1c104d45d8d85efa9b057b53b14b4b9b939dd74decc5321";

    private async Task<List<WorkingTreeChangedFile>?> TryGetStagedChangesAsync(
        string workTreeDirectory,
        GitObjectFormat objectFormat,
        GitObjectId? headTreeId,
        CancellationToken cancellationToken)
    {
        var comparisonTree = headTreeId?.Value ?? EmptyTree(objectFormat);
        var result = await _gitCliService
            .ExecuteBufferedAsync(
                [
                    "--no-optional-locks",
                    "diff",
                    "--cached",
                    "--name-status",
                    "-z",
                    "--find-renames",
                    comparisonTree,
                    "--",
                ],
                workTreeDirectory,
                validateExitCode: false,
                cancellationToken)
            .ConfigureAwait(false);
        return result.ExitCode == 0
            ? ParseStagedNameStatus(result.StandardOutput.AsSpan())
            : null;
    }

    internal static List<WorkingTreeChangedFile> ParseStagedNameStatus(ReadOnlySpan<char> output)
    {
        var changes = new List<WorkingTreeChangedFile>();
        var offset = 0;
        while (offset < output.Length)
        {
            var statusToken = ReadNulTerminated(output, ref offset);
            if (statusToken.Length == 0) break;
            var statusCode = statusToken[0];
            var firstPath = ReadNulTerminated(output, ref offset);
            var isRename = statusCode is 'R' or 'C';
            var path = isRename ? ReadNulTerminated(output, ref offset) : firstPath;
            if (statusCode == 'U') continue;
            changes.Add(Create(
                path,
                isRename ? firstPath : null,
                ToStatus(statusCode),
                WorkingTreeChangeGroup.Staged));
        }
        return changes;
    }

    private static string EmptyTree(GitObjectFormat objectFormat) => objectFormat switch
    {
        GitObjectFormat.Sha1 => Sha1EmptyTree,
        GitObjectFormat.Sha256 => Sha256EmptyTree,
        _ => throw new ArgumentOutOfRangeException(nameof(objectFormat), objectFormat, null),
    };
}
