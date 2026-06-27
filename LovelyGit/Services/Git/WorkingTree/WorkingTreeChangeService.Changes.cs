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
    private async Task<Dictionary<string, GitTreeFile>> ReadHeadFilesAsync(
        LovelyGitRepository repository,
        CancellationToken cancellationToken)
    {
        if (repository.HeadTarget == null)
        {
            return new Dictionary<string, GitTreeFile>(StringComparer.Ordinal);
        }

        var head = await repository.GetCommitAsync(repository.HeadTarget.Value, cancellationToken).ConfigureAwait(false);
        return head.TreeHash == null
            ? new Dictionary<string, GitTreeFile>(StringComparer.Ordinal)
            : await ReadTreeFilesAsync(repository, head.TreeHash.Value, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Dictionary<string, GitTreeFile>> ReadTreeFilesAsync(
        LovelyGitRepository repository,
        GitObjectId treeId,
        CancellationToken cancellationToken)
    {
        var comparison = await repository
            .GetChangedTreeFilesAsync(null, treeId, cancellationToken)
            .ConfigureAwait(false);
        return new Dictionary<string, GitTreeFile>(comparison.CurrentFiles, StringComparer.Ordinal);
    }

    private async Task<List<WorkingTreeChangedFile>> BuildStagedChangesAsync(
        LovelyGitRepository repository,
        IReadOnlyDictionary<string, GitTreeFile> headFiles,
        IReadOnlyDictionary<string, GitIndexEntry> indexEntries,
        CancellationToken cancellationToken)
    {
        var paths = headFiles.Keys.Concat(indexEntries.Keys).Distinct(StringComparer.Ordinal);
        var changes = new List<WorkingTreeChangedFile>();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            headFiles.TryGetValue(path, out var headFile);
            indexEntries.TryGetValue(path, out var indexEntry);
            if (headFile?.ObjectId == indexEntry?.ObjectId && headFile?.Mode == indexEntry?.Mode)
            {
                continue;
            }

            var oldBytes = headFile == null
                ? Array.Empty<byte>()
                : await TryReadBlobBytesAsync(repository, headFile.ObjectId, headFile.Mode, cancellationToken).ConfigureAwait(false);
            var newBytes = indexEntry == null
                ? Array.Empty<byte>()
                : await TryReadBlobBytesAsync(repository, indexEntry.ObjectId, indexEntry.Mode, cancellationToken).ConfigureAwait(false);
            var stats = CalculateStats(oldBytes, newBytes);
            changes.Add(new WorkingTreeChangedFile
            {
                Path = path,
                Status = headFile == null ? "Added" : indexEntry == null ? "Deleted" : "Modified",
                Group = WorkingTreeChangeGroup.Staged,
                Additions = stats.Additions,
                Deletions = stats.Deletions,
                IsBinary = stats.IsBinary,
            });
        }

        return changes.OrderBy(file => file.Path, StringComparer.Ordinal).ToList();
    }

    private async Task<List<WorkingTreeChangedFile>> BuildUnstagedChangesAsync(
        LovelyGitRepository repository,
        IReadOnlyDictionary<string, GitIndexEntry> indexEntries,
        CancellationToken cancellationToken)
    {
        var changes = new List<WorkingTreeChangedFile>();
        foreach (var entry in indexEntries.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(repository.WorkTreeDirectory, FromGitPath(entry.Path));
            if (!File.Exists(path))
            {
                var deletedOldBytes = await TryReadBlobBytesAsync(repository, entry.ObjectId, entry.Mode, cancellationToken).ConfigureAwait(false);
                var deletedStats = CalculateStats(deletedOldBytes, Array.Empty<byte>());
                changes.Add(new WorkingTreeChangedFile
                {
                    Path = entry.Path,
                    Status = "Deleted",
                    Group = WorkingTreeChangeGroup.Unstaged,
                    Additions = deletedStats.Additions,
                    Deletions = deletedStats.Deletions,
                    IsBinary = deletedStats.IsBinary,
                });
                continue;
            }

            var info = new FileInfo(path);
            if (entry.FileSize == info.Length
                && Math.Abs((info.LastWriteTimeUtc - entry.ModifiedTime.UtcDateTime).TotalSeconds) < 1)
            {
                continue;
            }

            var newBytes = await TryReadWorktreeFileBytesAsync(path, cancellationToken).ConfigureAwait(false);
            if (newBytes == null)
            {
                changes.Add(new WorkingTreeChangedFile
                {
                    Path = entry.Path,
                    Status = "Modified",
                    Group = WorkingTreeChangeGroup.Unstaged,
                    Additions = 0,
                    Deletions = 0,
                    IsBinary = true,
                });
                continue;
            }

            var newObjectId = ComputeBlobObjectId(newBytes, repository.ObjectFormat);
            if (newObjectId == entry.ObjectId)
            {
                continue;
            }

            var oldBytes = await TryReadBlobBytesAsync(repository, entry.ObjectId, entry.Mode, cancellationToken).ConfigureAwait(false);
            var stats = CalculateStats(oldBytes, newBytes);
            changes.Add(new WorkingTreeChangedFile
            {
                Path = entry.Path,
                Status = "Modified",
                Group = WorkingTreeChangeGroup.Unstaged,
                Additions = stats.Additions,
                Deletions = stats.Deletions,
                IsBinary = stats.IsBinary,
            });
        }

        return changes.OrderBy(file => file.Path, StringComparer.Ordinal).ToList();
    }

    private static async Task<byte[]?> TryReadWorktreeFileBytesAsync(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var length = file.Length <= int.MaxValue ? (int)file.Length : 0;
            using var output = length > 0 ? new MemoryStream(length) : new MemoryStream();
            await file.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            return output.ToArray();
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested && exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private async Task<List<WorkingTreeChangedFile>> BuildUntrackedChangesAsync(
        LovelyGitRepository repository,
        IEnumerable<string> trackedPaths,
        CancellationToken cancellationToken)
    {
        var tracked = new HashSet<string>(trackedPaths, StringComparer.Ordinal);
        var matcher = await GitIgnoreMatcher
            .LoadAsync(repository.WorkTreeDirectory, repository.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var changes = new List<WorkingTreeChangedFile>();
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(repository.WorkTreeDirectory);

        while (pendingDirectories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pendingDirectories.Pop();
            var relativeDirectory = NormalizePath(Path.GetRelativePath(repository.WorkTreeDirectory, directory));
            if (relativeDirectory == ".")
            {
                relativeDirectory = string.Empty;
            }

            if (!string.IsNullOrEmpty(relativeDirectory))
            {
                await matcher
                    .LoadRulesForDirectoryAsync(repository.WorkTreeDirectory, relativeDirectory, cancellationToken)
                    .ConfigureAwait(false);
            }

            foreach (var childDirectory in SafeEnumerateDirectories(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relative = NormalizePath(Path.GetRelativePath(repository.WorkTreeDirectory, childDirectory));
                if (relative.Equals(".git", StringComparison.Ordinal)
                    || relative.StartsWith(".git/", StringComparison.Ordinal)
                    || matcher.IsIgnored(relative, true))
                {
                    continue;
                }

                pendingDirectories.Push(childDirectory);
            }

            foreach (var filePath in SafeEnumerateFiles(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relative = NormalizePath(Path.GetRelativePath(repository.WorkTreeDirectory, filePath));
                if (relative.StartsWith(".git/", StringComparison.Ordinal)
                    || tracked.Contains(relative)
                    || matcher.IsIgnored(relative, false))
                {
                    continue;
                }

                changes.Add(new WorkingTreeChangedFile
                {
                    Path = relative,
                    Status = "Added",
                    Group = WorkingTreeChangeGroup.Untracked,
                    Additions = 0,
                    Deletions = 0,
                    IsBinary = false,
                });
            }
        }

        return changes.OrderBy(file => file.Path, StringComparer.Ordinal).ToList();
    }

}
