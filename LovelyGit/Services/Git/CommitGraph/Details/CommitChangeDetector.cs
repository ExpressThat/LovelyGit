using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;

internal sealed class CommitChangeDetector
{
    private readonly BlobLineAnalyzer _blobLineAnalyzer;

    public CommitChangeDetector(BlobLineAnalyzer blobLineAnalyzer)
    {
        _blobLineAnalyzer = blobLineAnalyzer;
    }

    public async Task<List<CommitChangedFile>> BuildChangedFilesAsync(
        IReadOnlyDictionary<string, GitTreeFile> parentFiles,
        IReadOnlyDictionary<string, GitTreeFile> currentFiles,
        CancellationToken cancellationToken)
    {
        var changedFiles = new List<CommitChangedFile>();
        var deleted = new Dictionary<string, GitTreeFile>(StringComparer.Ordinal);
        var added = new Dictionary<string, GitTreeFile>(StringComparer.Ordinal);

        foreach (var (path, parentFile) in parentFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!currentFiles.TryGetValue(path, out var currentFile))
            {
                deleted[path] = parentFile;
                continue;
            }

            if (parentFile.ObjectId == currentFile.ObjectId && parentFile.Mode == currentFile.Mode)
            {
                continue;
            }

            var status = IsSameFileKind(parentFile, currentFile) ? "Modified" : "TypeChanged";
            changedFiles.Add(await BuildComparedFileAsync(
                    status,
                    currentFile.Path,
                    parentFile,
                    currentFile,
                    cancellationToken)
                .ConfigureAwait(false));
        }

        foreach (var (path, currentFile) in currentFiles)
        {
            if (!parentFiles.ContainsKey(path))
            {
                added[path] = currentFile;
            }
        }

        foreach (var file in deleted.Values)
        {
            changedFiles.Add(await BuildDeletedFileAsync(file, cancellationToken).ConfigureAwait(false));
        }

        foreach (var file in added.Values)
        {
            changedFiles.Add(await BuildAddedFileAsync(file, cancellationToken).ConfigureAwait(false));
        }

        return changedFiles
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToList();
    }

    private async Task<CommitChangedFile> BuildComparedFileAsync(
        string status,
        string path,
        GitTreeFile oldFile,
        GitTreeFile newFile,
        CancellationToken cancellationToken)
    {
        var oldBlob = await _blobLineAnalyzer.AnalyzeAsync(oldFile, cancellationToken).ConfigureAwait(false);
        var newBlob = await _blobLineAnalyzer.AnalyzeAsync(newFile, cancellationToken).ConfigureAwait(false);
        var stats = BlobLineAnalyzer.CalculateLineStats(oldBlob, newBlob);
        return new CommitChangedFile
        {
            Path = path,
            Status = status,
            Additions = stats.Additions,
            Deletions = stats.Deletions,
            IsBinary = oldBlob.IsBinary || newBlob.IsBinary,
        };
    }

    private async Task<CommitChangedFile> BuildAddedFileAsync(
        GitTreeFile file,
        CancellationToken cancellationToken)
    {
        var blob = await _blobLineAnalyzer.AnalyzeAsync(file, cancellationToken).ConfigureAwait(false);
        return new CommitChangedFile
        {
            Path = file.Path,
            Status = "Added",
            Additions = blob.IsBinary ? 0u : (uint)blob.Lines.Length,
            Deletions = 0,
            IsBinary = blob.IsBinary,
        };
    }

    private async Task<CommitChangedFile> BuildDeletedFileAsync(
        GitTreeFile file,
        CancellationToken cancellationToken)
    {
        var blob = await _blobLineAnalyzer.AnalyzeAsync(file, cancellationToken).ConfigureAwait(false);
        return new CommitChangedFile
        {
            Path = file.Path,
            Status = "Deleted",
            Additions = 0,
            Deletions = blob.IsBinary ? 0u : (uint)blob.Lines.Length,
            IsBinary = blob.IsBinary,
        };
    }

    private static bool IsSameFileKind(GitTreeFile left, GitTreeFile right)
    {
        return IsSymlink(left) == IsSymlink(right) && IsSubmodule(left) == IsSubmodule(right);
    }

    private static bool IsSymlink(GitTreeFile file) => file.Mode == "120000";

    private static bool IsSubmodule(GitTreeFile file) => file.Mode == "160000";
}
