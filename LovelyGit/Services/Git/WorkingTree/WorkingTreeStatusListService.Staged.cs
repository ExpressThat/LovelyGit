using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeStatusListService
{
    private static async Task<bool> HasStagedChangesAsync(
        string worktreeGitDirectory,
        string commonGitDirectory,
        GitObjectFormat objectFormat,
        GitIndexStatusScan scan,
        CancellationToken cancellationToken)
    {
        if (scan.Response.Unmerged.Count > 0)
        {
            return false;
        }

        var headTreeId = await ReadHeadTreeIdAsync(
            worktreeGitDirectory, commonGitDirectory, objectFormat, cancellationToken)
            .ConfigureAwait(false);
        return headTreeId == null
            ? scan.RootTreeId != null
            : scan.RootTreeId != headTreeId;
    }

    private static async Task<GitObjectId?> ReadHeadTreeIdAsync(
        string worktreeGitDirectory,
        string commonGitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var headTarget = await ReadHeadTargetAsync(
            worktreeGitDirectory, commonGitDirectory, objectFormat, cancellationToken)
            .ConfigureAwait(false);
        if (headTarget == null)
        {
            return null;
        }

        using var objectStore = new GitObjectStore(commonGitDirectory, objectFormat);
        var data = await objectStore.ReadObjectAsync(headTarget.Value, cancellationToken)
            .ConfigureAwait(false);
        return data.Kind == GitObjectKind.Commit
            ? GitObjectParsers.ParseCommit(headTarget.Value, data.Data).TreeHash
            : null;
    }

    private static async Task<GitObjectId?> ReadHeadTargetAsync(
        string worktreeGitDirectory,
        string commonGitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var headPath = Path.Combine(worktreeGitDirectory, "HEAD");
        if (!File.Exists(headPath))
        {
            return null;
        }

        var text = (await File.ReadAllTextAsync(headPath, cancellationToken).ConfigureAwait(false)).Trim();
        const string RefPrefix = "ref:";
        if (!text.StartsWith(RefPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return GitObjectId.TryParse(text, objectFormat, out var detachedId) ? detachedId : null;
        }

        var refName = text.AsSpan(RefPrefix.Length).Trim().ToString();
        return await ReadRefTargetAsync(commonGitDirectory, objectFormat, refName, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<GitObjectId?> ReadRefTargetAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        string refName,
        CancellationToken cancellationToken)
    {
        var looseRefPath = Path.Combine(gitDirectory, refName.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(looseRefPath))
        {
            var text = (await File.ReadAllTextAsync(looseRefPath, cancellationToken).ConfigureAwait(false)).Trim();
            return GitObjectId.TryParse(text, objectFormat, out var id) ? id : null;
        }

        return ReadPackedRefTarget(gitDirectory, objectFormat, refName, cancellationToken);
    }

    private static GitObjectId? ReadPackedRefTarget(
        string gitDirectory,
        GitObjectFormat objectFormat,
        string refName,
        CancellationToken cancellationToken)
    {
        var packedRefsPath = Path.Combine(gitDirectory, "packed-refs");
        if (!File.Exists(packedRefsPath))
        {
            return null;
        }

        foreach (var line in File.ReadLines(packedRefsPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (line.Length == 0 || line[0] is '#' or '^')
            {
                continue;
            }

            var spaceIndex = line.IndexOf(' ');
            if (spaceIndex > 0
                && line.AsSpan(spaceIndex + 1).SequenceEqual(refName)
                && GitObjectId.TryParse(line.AsSpan(0, spaceIndex), objectFormat, out var id))
            {
                return id;
            }
        }

        return null;
    }
}
