using System.Security.Cryptography;
using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
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
        var indexEntries = await new GitIndexReader()
            .ReadAsync(repository.GitDirectory, repository.ObjectFormat, cancellationToken)
            .ConfigureAwait(false);
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

        var headFiles = await ReadHeadFilesAsync(repository, cancellationToken).ConfigureAwait(false);
        var staged = await BuildStagedChangesAsync(repository, headFiles, normalIndexEntries, cancellationToken)
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
        CancellationToken cancellationToken)
    {
        path = NormalizePath(path);
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var indexEntries = await new GitIndexReader()
            .ReadAsync(repository.GitDirectory, repository.ObjectFormat, cancellationToken)
            .ConfigureAwait(false);
        var indexByPath = indexEntries
            .Where(entry => entry.Stage == 0)
            .ToDictionary(entry => entry.Path, StringComparer.Ordinal);
        var headFiles = await ReadHeadFilesAsync(repository, cancellationToken).ConfigureAwait(false);

        byte[] oldBytes = Array.Empty<byte>();
        byte[] newBytes = Array.Empty<byte>();
        var status = "Modified";

        if (group == WorkingTreeChangeGroup.Staged)
        {
            headFiles.TryGetValue(path, out var headFile);
            indexByPath.TryGetValue(path, out var indexEntry);
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
            var readBytes = File.Exists(worktreePath)
                ? await TryReadWorktreeFileBytesAsync(worktreePath, cancellationToken).ConfigureAwait(false)
                : null;
            if (File.Exists(worktreePath) && readBytes == null)
            {
                return BuildUnreadableFileDiff("WORKTREE", path, "Modified", viewMode);
            }

            newBytes = readBytes ?? Array.Empty<byte>();
            status = File.Exists(worktreePath) ? "Modified" : "Deleted";
        }
        else if (group == WorkingTreeChangeGroup.Untracked)
        {
            var worktreePath = Path.Combine(repository.WorkTreeDirectory, FromGitPath(path));
            if (indexByPath.TryGetValue(path, out var indexEntry))
            {
                oldBytes = await TryReadBlobBytesAsync(repository, indexEntry.ObjectId, indexEntry.Mode, cancellationToken).ConfigureAwait(false) ?? Array.Empty<byte>();
                var readBytes = File.Exists(worktreePath)
                    ? await TryReadWorktreeFileBytesAsync(worktreePath, cancellationToken).ConfigureAwait(false)
                    : null;
                if (File.Exists(worktreePath) && readBytes == null)
                {
                    return BuildUnreadableFileDiff("WORKTREE", path, "Modified", viewMode);
                }

                newBytes = readBytes ?? Array.Empty<byte>();
                status = File.Exists(worktreePath) ? "Modified" : "Deleted";
                return BuildDiffResponse("WORKTREE", path, status, viewMode, oldBytes, newBytes);
            }

            var untrackedBytes = await TryReadWorktreeFileBytesAsync(worktreePath, cancellationToken).ConfigureAwait(false);
            if (untrackedBytes == null)
            {
                return BuildUnreadableFileDiff("WORKTREE", path, "Added", viewMode);
            }

            newBytes = untrackedBytes;
            status = "Added";
        }
        else
        {
            throw new InvalidOperationException("Unmerged file diffs are not available yet.");
        }

        return BuildDiffResponse("WORKTREE", path, status, viewMode, oldBytes, newBytes);
    }

}
