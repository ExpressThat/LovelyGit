using System.Security.Cryptography;
using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
using ExpressThat.LovelyGit.Services.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;
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
        using var trace = LovelyGitTrace.Time(
            "working-tree.file-diff",
            $"{group} {viewMode} {path}");
        path = NormalizePath(path);
        if (group == WorkingTreeChangeGroup.Untracked)
        {
            var paths = await GitRepositoryDiscovery
                .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
                .ConfigureAwait(false);
            return await BuildUntrackedFileDiffAsync(
                    paths.WorkTreeDirectory,
                    path,
                    viewMode,
                    ignoreWhitespace,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        if (group == WorkingTreeChangeGroup.Unstaged)
        {
            return await BuildUnstagedFileDiffAsync(
                    repositoryPath, path, viewMode, ignoreWhitespace, cancellationToken)
                .ConfigureAwait(false);
        }
        if (group != WorkingTreeChangeGroup.Staged)
        {
            throw new InvalidOperationException("Unmerged file diffs are not available yet.");
        }

        return await BuildStagedFileDiffAsync(
                repositoryPath, path, viewMode, ignoreWhitespace, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<CommitFileDiffResponse> BuildStagedFileDiffAsync(
        string repositoryPath,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
            repositoryPath, cancellationToken).ConfigureAwait(false);
        var objectFormat = await GitRepositoryDiscovery.ReadObjectFormatAsync(
            paths.GitDirectory, cancellationToken).ConfigureAwait(false);
        var entries = await new GitIndexReader().ReadEntriesForPathAsync(
            paths.WorktreeGitDirectory, objectFormat, path, cancellationToken).ConfigureAwait(false);
        var indexEntry = entries.FirstOrDefault(entry => entry.Stage == 0);
        var head = await GitHeadReader.ReadAsync(
            paths.WorktreeGitDirectory, paths.GitDirectory, objectFormat, cancellationToken)
            .ConfigureAwait(false);
        using var objectStore = new GitObjectStore(paths.GitDirectory, objectFormat);
        GitTreeFile? headFile = null;
        if (head.Target is { } headTarget)
        {
            var commitData = await objectStore.ReadObjectAsync(headTarget, cancellationToken)
                .ConfigureAwait(false);
            if (commitData.Kind != GitObjectKind.Commit)
            {
                throw new InvalidDataException($"Object is not a commit: {headTarget}");
            }

            var treeId = GitObjectParsers
                .ParseCommitTraversalHeader(headTarget, commitData.Data)
                .TreeHash;
            if (treeId is { } value)
            {
                headFile = await objectStore.TryGetTreeFileAsync(value, path, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        var oldBytes = headFile == null
            ? Array.Empty<byte>()
            : await TryReadBlobBytesAsync(
                objectStore, headFile.ObjectId, headFile.Mode, cancellationToken).ConfigureAwait(false)
                ?? Array.Empty<byte>();
        var newBytes = indexEntry == null
            ? Array.Empty<byte>()
            : await TryReadBlobBytesAsync(
                objectStore, indexEntry.ObjectId, indexEntry.Mode, cancellationToken).ConfigureAwait(false)
                ?? Array.Empty<byte>();
        var status = headFile == null ? "Added" : indexEntry == null ? "Deleted" : "Modified";
        return BuildDiffResponse(
            "WORKTREE", path, status, viewMode, ignoreWhitespace, oldBytes, newBytes);
    }

    private static async Task<CommitFileDiffResponse> BuildUnstagedFileDiffAsync(
        string repositoryPath,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
            repositoryPath, cancellationToken).ConfigureAwait(false);
        var objectFormat = await GitRepositoryDiscovery.ReadObjectFormatAsync(
            paths.GitDirectory, cancellationToken).ConfigureAwait(false);
        var entries = await new GitIndexReader().ReadEntriesForPathAsync(
            paths.WorktreeGitDirectory, objectFormat, path, cancellationToken).ConfigureAwait(false);
        var indexEntry = entries.FirstOrDefault(entry => entry.Stage == 0)
            ?? throw new FileNotFoundException("Index entry not found.", path);
        using var objectStore = new GitObjectStore(paths.GitDirectory, objectFormat);
        var oldBytes = await TryReadBlobBytesAsync(
            objectStore, indexEntry.ObjectId, indexEntry.Mode, cancellationToken).ConfigureAwait(false)
            ?? Array.Empty<byte>();
        var worktreePath = Path.Combine(paths.WorkTreeDirectory, FromGitPath(path));
        var exists = File.Exists(worktreePath);
        var newBytes = exists
            ? await TryReadWorktreeFileBytesAsync(worktreePath, cancellationToken).ConfigureAwait(false)
            : null;
        if (exists && newBytes == null)
        {
            return BuildUnreadableFileDiff("WORKTREE", path, "Modified", viewMode);
        }

        return BuildDiffResponse(
            "WORKTREE",
            path,
            exists ? "Modified" : "Deleted",
            viewMode,
            ignoreWhitespace,
            oldBytes,
            newBytes ?? Array.Empty<byte>());
    }

    private static async Task<CommitFileDiffResponse> BuildUntrackedFileDiffAsync(
        string workTreeDirectory,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        var worktreePath = Path.Combine(workTreeDirectory, FromGitPath(path));
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

}
