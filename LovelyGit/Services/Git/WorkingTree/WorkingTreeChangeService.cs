using System.Security.Cryptography;
using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeChangeService
{
    private const int MaxSyntaxHighlightedCharacters = 750_000;
    private const int MaxSyntaxHighlightedLineLength = 2_000;

    public async Task<WorkingTreeChangesResponse> GetChangesAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var indexSnapshot = await new GitIndexReader()
            .ReadSnapshotAsync(repository.GitDirectory, repository.ObjectFormat, cancellationToken)
            .ConfigureAwait(false);
        var indexEntries = indexSnapshot.Entries;
        var normalIndexEntries = indexEntries
            .Where(entry => entry.Stage == 0 && !entry.SkipWorkTree && !entry.IntentToAdd)
            .ToDictionary(entry => entry.Path, StringComparer.Ordinal);
        var unmerged = indexEntries
            .Where(entry => entry.Stage != 0)
            .GroupBy(entry => entry.Path, StringComparer.Ordinal)
            .Select(group => new WorkingTreeChangedFile
            {
                Path = group.Key,
                Status = "Unmerged",
                Group = WorkingTreeChangeGroup.Unmerged,
            })
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToList();

        var head = repository.HeadTarget == null
            ? null
            : await repository.GetCommitAsync(repository.HeadTarget.Value, cancellationToken).ConfigureAwait(false);
        var staged = head?.TreeHash != null && indexSnapshot.RootTreeId == head.TreeHash
            ? new List<WorkingTreeChangedFile>()
            : await BuildStagedChangesAsync(
                    repository,
                    head?.TreeHash == null
                        ? new Dictionary<string, GitTreeFile>(StringComparer.Ordinal)
                        : await ReadTreeFilesAsync(repository, head.TreeHash.Value, cancellationToken).ConfigureAwait(false),
                    normalIndexEntries,
                    cancellationToken)
                .ConfigureAwait(false);
        var unstaged = await BuildUnstagedChangesAsync(repository, normalIndexEntries, cancellationToken)
            .ConfigureAwait(false);
        var untracked = await BuildUntrackedChangesAsync(repository, normalIndexEntries.Keys, cancellationToken)
            .ConfigureAwait(false);

        return new WorkingTreeChangesResponse
        {
            Staged = staged,
            Unstaged = unstaged,
            Untracked = untracked,
            Unmerged = unmerged,
        };
    }

    public async Task<CommitFileDiffResponse> GetFileDiffAsync(
        string repositoryPath,
        string path,
        WorkingTreeChangeGroup group,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        path = NormalizePath(path);
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        if (group == WorkingTreeChangeGroup.Untracked)
        {
            return await BuildUntrackedFileDiffAsync(
                    repository.WorkTreeDirectory,
                    path,
                    viewMode,
                    ignoreWhitespace,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var indexEntries = await new GitIndexReader()
            .ReadAsync(repository.GitDirectory, repository.ObjectFormat, cancellationToken)
            .ConfigureAwait(false);
        var indexByPath = indexEntries
            .Where(entry => entry.Stage == 0)
            .ToDictionary(entry => entry.Path, StringComparer.Ordinal);

        byte[] oldBytes = Array.Empty<byte>();
        byte[] newBytes = Array.Empty<byte>();
        var status = "Modified";

        if (group == WorkingTreeChangeGroup.Staged)
        {
            var headFiles = await ReadHeadFilesAsync(repository, cancellationToken).ConfigureAwait(false);
            headFiles.TryGetValue(path, out var headFile);
            indexByPath.TryGetValue(path, out var indexEntry);
            if (indexEntry != null && IsOversizedDiffInput(0, indexEntry.FileSize))
            {
                return BuildLargeFileDiff(path, headFile == null ? "Added" : "Modified", viewMode, 0, indexEntry.FileSize);
            }

            oldBytes = headFile == null
                ? Array.Empty<byte>()
                : await TryReadBlobBytesAsync(repository, headFile.ObjectId, headFile.Mode, cancellationToken).ConfigureAwait(false) ?? Array.Empty<byte>();
            newBytes = indexEntry == null
                ? Array.Empty<byte>()
                : await TryReadBlobBytesAsync(repository, indexEntry.ObjectId, indexEntry.Mode, cancellationToken).ConfigureAwait(false) ?? Array.Empty<byte>();
            status = headFile == null ? "Added" : indexEntry == null ? "Deleted" : "Modified";
        }
        else if (group == WorkingTreeChangeGroup.Unstaged)
        {
            if (!indexByPath.TryGetValue(path, out var indexEntry))
            {
                throw new FileNotFoundException("Index entry not found.", path);
            }

            oldBytes = await TryReadBlobBytesAsync(repository, indexEntry.ObjectId, indexEntry.Mode, cancellationToken).ConfigureAwait(false) ?? Array.Empty<byte>();
            var worktreePath = Path.Combine(repository.WorkTreeDirectory, FromGitPath(path));
            var worktreeInfo = new FileInfo(worktreePath);
            if (worktreeInfo.Exists
                && IsOversizedDiffInput(indexEntry.FileSize, worktreeInfo.Length))
            {
                return BuildLargeFileDiff(path, "Modified", viewMode, indexEntry.FileSize, worktreeInfo.Length);
            }

            var readBytes = worktreeInfo.Exists
                ? await TryReadWorktreeFileBytesAsync(worktreePath, cancellationToken).ConfigureAwait(false)
                : null;
            if (worktreeInfo.Exists && readBytes == null)
            {
                return BuildUnreadableFileDiff("WORKTREE", path, "Modified", viewMode);
            }

            newBytes = readBytes ?? Array.Empty<byte>();
            status = worktreeInfo.Exists ? "Modified" : "Deleted";
        }
        else
        {
            throw new InvalidOperationException("Unmerged file diffs are not available yet.");
        }

        return BuildDiffResponse("WORKTREE", path, status, viewMode, ignoreWhitespace, oldBytes, newBytes);
    }

    private static async Task<CommitFileDiffResponse> BuildUntrackedFileDiffAsync(
        string workTreeDirectory,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        var worktreePath = Path.Combine(workTreeDirectory, FromGitPath(path));
        var worktreeInfo = new FileInfo(worktreePath);
        if (worktreeInfo.Exists && IsOversizedDiffInput(0, worktreeInfo.Length))
        {
            return BuildLargeFileDiff(path, "Added", viewMode, 0, worktreeInfo.Length);
        }

        var untrackedBytes = await TryReadWorktreeFileBytesAsync(worktreePath, cancellationToken)
            .ConfigureAwait(false);
        if (untrackedBytes == null)
        {
            return BuildUnreadableFileDiff("WORKTREE", path, "Added", viewMode);
        }

        return BuildDiffResponse(
            "WORKTREE",
            path,
            "Added",
            viewMode,
            ignoreWhitespace,
            Array.Empty<byte>(),
            untrackedBytes);
    }

    private static bool IsOversizedDiffInput(long oldByteCount, long newByteCount) =>
        DiffInputGuard.ShouldTruncateBytes(oldByteCount, newByteCount);

    private static CommitFileDiffResponse BuildLargeFileDiff(
        string path,
        string status,
        CommitDiffViewMode viewMode,
        long oldByteCount,
        long newByteCount) =>
        DiffInputGuard.BuildTruncatedResponse(
            "WORKTREE",
            path,
            status,
            viewMode,
            oldByteCount,
            newByteCount);

}
