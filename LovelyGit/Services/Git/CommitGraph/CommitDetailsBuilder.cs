using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitDetailsBuilder
{
    private readonly LovelyGitRepository _repository;

    public CommitDetailsBuilder(LovelyGitRepository repository)
    {
        _repository = repository;
    }

    public async Task<CommitDetailsResponse> BuildAsync(
        GitCommit commit,
        GitCommit? firstParent,
        CancellationToken cancellationToken)
    {
        var comparison = await _repository.GetChangedTreeFilesAsync(firstParent?.TreeHash, commit.TreeHash, cancellationToken)
            .ConfigureAwait(false);
        var changedFiles = await BuildChangedFilesAsync(
                comparison.ParentFiles,
                comparison.CurrentFiles,
                cancellationToken)
            .ConfigureAwait(false);

        return new CommitDetailsResponse
        {
            Hash = commit.Hash.ToString(),
            Parents = commit.ParentHashes.Select(parent => parent.ToString()).ToList(),
            Author = string.IsNullOrWhiteSpace(commit.AuthorName) ? "unknown" : commit.AuthorName,
            Email = commit.AuthorEmail,
            Date = commit.AuthorUnixSeconds,
            Subject = commit.Subject,
            Body = commit.Body,
            Message = commit.Body.Trim('\r', '\n'),
            Branches = commit.Branches.ToList(),
            Tags = commit.Tags.ToList(),
            Stats = new CommitStats
            {
                Additions = changedFiles.Aggregate(0u, (total, file) => total + file.Additions),
                Deletions = changedFiles.Aggregate(0u, (total, file) => total + file.Deletions),
            },
            ChangedFiles = changedFiles,
        };
    }

    private async Task<List<CommitChangedFile>> BuildChangedFilesAsync(
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
        var oldBlob = await AnalyzeBlobAsync(oldFile, cancellationToken).ConfigureAwait(false);
        var newBlob = await AnalyzeBlobAsync(newFile, cancellationToken).ConfigureAwait(false);
        var stats = CalculateLineStats(oldBlob, newBlob);
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
        var blob = await AnalyzeBlobAsync(file, cancellationToken).ConfigureAwait(false);
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
        var blob = await AnalyzeBlobAsync(file, cancellationToken).ConfigureAwait(false);
        return new CommitChangedFile
        {
            Path = file.Path,
            Status = "Deleted",
            Additions = 0,
            Deletions = blob.IsBinary ? 0u : (uint)blob.Lines.Length,
            IsBinary = blob.IsBinary,
        };
    }

    private async Task<BlobAnalysis> AnalyzeBlobAsync(
        GitTreeFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await _repository.ReadBlobAsync(file.ObjectId, cancellationToken).ConfigureAwait(false);
            var isBinary = IsBinary(bytes);
            return isBinary
                ? new BlobAnalysis(bytes, isBinary, Array.Empty<LineFingerprint>())
                : new BlobAnalysis(bytes, isBinary, BuildLineFingerprints(bytes));
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return new BlobAnalysis(Array.Empty<byte>(), IsBinary: true, Array.Empty<LineFingerprint>());
        }
    }

    private static bool IsSameFileKind(GitTreeFile left, GitTreeFile right)
    {
        return IsSymlink(left) == IsSymlink(right) && IsSubmodule(left) == IsSubmodule(right);
    }

    private static bool IsSymlink(GitTreeFile file) => file.Mode == "120000";

    private static bool IsSubmodule(GitTreeFile file) => file.Mode == "160000";

    private static (uint Additions, uint Deletions) CalculateLineStats(
        BlobAnalysis oldBlob,
        BlobAnalysis newBlob)
    {
        if (oldBlob.IsBinary || newBlob.IsBinary)
        {
            return (0, 0);
        }

        var common = CountCommonLines(oldBlob.Lines, newBlob.Lines);
        return ((uint)(newBlob.Lines.Length - common), (uint)(oldBlob.Lines.Length - common));
    }

    private static int CountCommonLines(LineFingerprint[] oldLines, LineFingerprint[] newLines)
    {
        if (oldLines.Length == 0 || newLines.Length == 0)
        {
            return 0;
        }

        var counts = new Dictionary<LineFingerprint, int>();
        foreach (var line in oldLines)
        {
            counts.TryGetValue(line, out var count);
            counts[line] = count + 1;
        }

        var common = 0;
        foreach (var line in newLines)
        {
            if (!counts.TryGetValue(line, out var count) || count == 0)
            {
                continue;
            }

            common++;
            if (count == 1)
            {
                counts.Remove(line);
            }
            else
            {
                counts[line] = count - 1;
            }
        }

        return common;
    }

    private static bool IsBinary(byte[] bytes)
    {
        var length = Math.Min(bytes.Length, 8000);
        for (var i = 0; i < length; i++)
        {
            if (bytes[i] == 0)
            {
                return true;
            }
        }

        return false;
    }

    private static LineFingerprint[] BuildLineFingerprints(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return Array.Empty<LineFingerprint>();
        }

        var lines = new List<LineFingerprint>();
        var start = 0;
        for (var index = 0; index < bytes.Length; index++)
        {
            if (bytes[index] != (byte)'\n')
            {
                continue;
            }

            AddLineFingerprint(bytes.AsSpan(start, index - start), lines);
            start = index + 1;
        }

        if (start < bytes.Length)
        {
            AddLineFingerprint(bytes.AsSpan(start), lines);
        }

        return lines.ToArray();
    }

    private static void AddLineFingerprint(ReadOnlySpan<byte> line, List<LineFingerprint> lines)
    {
        if (line.Length > 0 && line[^1] == (byte)'\r')
        {
            line = line[..^1];
        }

        lines.Add(new LineFingerprint(HashLine(line), line.Length));
    }

    private static ulong HashLine(ReadOnlySpan<byte> line)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        var hash = offset;
        foreach (var value in line)
        {
            hash ^= value;
            hash *= prime;
        }

        return hash;
    }

    private readonly record struct LineFingerprint(ulong Hash, int Length);

    private sealed record BlobAnalysis(byte[] Bytes, bool IsBinary, LineFingerprint[] Lines);
}
