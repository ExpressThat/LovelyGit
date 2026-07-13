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
        var changedFiles = new List<ChangedFileWorkItem>(Math.Max(parentFiles.Count, currentFiles.Count));

        foreach (var (path, parentFile) in parentFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!currentFiles.TryGetValue(path, out var currentFile))
            {
                changedFiles.Add(ChangedFileWorkItem.Deleted(parentFile));
                continue;
            }

            if (parentFile.ObjectId == currentFile.ObjectId && parentFile.Mode == currentFile.Mode)
            {
                continue;
            }

            var status = IsSameFileKind(parentFile, currentFile) ? "Modified" : "TypeChanged";
            changedFiles.Add(ChangedFileWorkItem.Compared(
                    status,
                    currentFile.Path,
                    parentFile,
                    currentFile));
        }

        foreach (var (path, currentFile) in currentFiles)
        {
            if (!parentFiles.ContainsKey(path))
            {
                changedFiles.Add(ChangedFileWorkItem.Added(currentFile));
            }
        }

        changedFiles.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Path, right.Path));
        var results = new CommitChangedFile[changedFiles.Count];
        await Parallel.ForEachAsync(
                Enumerable.Range(0, changedFiles.Count),
                cancellationToken,
                async (index, itemCancellationToken) =>
                {
                    var item = changedFiles[index];
                    results[index] = item.Kind switch
                    {
                        ChangedFileKind.Compared => await BuildComparedFileAsync(
                                item.Status,
                                item.Path,
                                item.OldFile,
                                item.NewFile,
                                itemCancellationToken)
                            .ConfigureAwait(false),
                        ChangedFileKind.Added => await BuildAddedFileAsync(item.NewFile, itemCancellationToken)
                            .ConfigureAwait(false),
                        ChangedFileKind.Deleted => await BuildDeletedFileAsync(item.OldFile, itemCancellationToken)
                            .ConfigureAwait(false),
                        _ => throw new InvalidOperationException("Unknown changed file work item kind."),
                    };
                })
            .ConfigureAwait(false);

        return [.. results];
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
        var blob = await _blobLineAnalyzer.SummarizeAsync(file, cancellationToken).ConfigureAwait(false);
        return new CommitChangedFile
        {
            Path = file.Path,
            Status = "Added",
            Additions = blob.IsBinary ? 0u : (uint)blob.LineCount,
            Deletions = 0,
            IsBinary = blob.IsBinary,
        };
    }

    private async Task<CommitChangedFile> BuildDeletedFileAsync(
        GitTreeFile file,
        CancellationToken cancellationToken)
    {
        var blob = await _blobLineAnalyzer.SummarizeAsync(file, cancellationToken).ConfigureAwait(false);
        return new CommitChangedFile
        {
            Path = file.Path,
            Status = "Deleted",
            Additions = 0,
            Deletions = blob.IsBinary ? 0u : (uint)blob.LineCount,
            IsBinary = blob.IsBinary,
        };
    }

    private static bool IsSameFileKind(GitTreeFile left, GitTreeFile right)
    {
        return IsSymlink(left) == IsSymlink(right) && IsSubmodule(left) == IsSubmodule(right);
    }

    private static bool IsSymlink(GitTreeFile file) => file.Mode == "120000";

    private static bool IsSubmodule(GitTreeFile file) => file.Mode == "160000";

    private enum ChangedFileKind
    {
        Compared,
        Added,
        Deleted,
    }

    private readonly record struct ChangedFileWorkItem(
        ChangedFileKind Kind,
        string Status,
        string Path,
        GitTreeFile OldFile,
        GitTreeFile NewFile)
    {
        public static ChangedFileWorkItem Compared(
            string status,
            string path,
            GitTreeFile oldFile,
            GitTreeFile newFile)
        {
            return new ChangedFileWorkItem(ChangedFileKind.Compared, status, path, oldFile, newFile);
        }

        public static ChangedFileWorkItem Added(GitTreeFile file)
        {
            return new ChangedFileWorkItem(ChangedFileKind.Added, "Added", file.Path, file, file);
        }

        public static ChangedFileWorkItem Deleted(GitTreeFile file)
        {
            return new ChangedFileWorkItem(ChangedFileKind.Deleted, "Deleted", file.Path, file, file);
        }
    }
}
