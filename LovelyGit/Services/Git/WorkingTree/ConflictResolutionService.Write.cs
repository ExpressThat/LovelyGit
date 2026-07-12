using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionService
{
    private readonly WorkingTreeIndexService _indexService;
    private readonly ConflictResolutionResponseCache _responseCache = new();

    public ConflictResolutionService(WorkingTreeIndexService indexService)
    {
        _indexService = indexService;
    }

    public async Task ResolveAsync(
        string repositoryPath,
        string path,
        string expectedFingerprint,
        string? resultText,
        ConflictResolutionSource? source,
        bool deleteResult,
        CancellationToken cancellationToken)
    {
        path = WorkingTreePath.NormalizeRelative(path);
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var entries = await ReadConflictEntriesAsync(repository, path, cancellationToken).ConfigureAwait(false);
        var targetPath = WorkingTreePath.Resolve(repository.WorkTreeDirectory, path);
        if (File.Exists(targetPath) && File.GetAttributes(targetPath).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidOperationException("Symbolic-link conflicts are not supported yet.");
        }

        if (!string.Equals(
                expectedFingerprint,
                await ConflictFingerprintAsync(targetPath, entries, cancellationToken).ConfigureAwait(false),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The result changed after the resolver opened. Reload it before resolving.");
        }

        var bytes = await SelectResultAsync(repository, entries, resultText, source, deleteResult, cancellationToken)
            .ConfigureAwait(false);
        var backupPath = targetPath + $".lovelygit-backup-{Guid.NewGuid():N}";
        var temporaryPath = targetPath + $".lovelygit-result-{Guid.NewGuid():N}";
        var hadOriginal = File.Exists(targetPath);
        var staged = false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            if (bytes != null)
            {
                await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                if (hadOriginal)
                {
                    File.Move(targetPath, backupPath);
                }

                if (bytes != null)
                {
                    File.Move(temporaryPath, targetPath);
                }

                await _indexService.StageAsync(repositoryPath, [path], includeAll: false, cancellationToken)
                    .ConfigureAwait(false);
                staged = true;
            }
            catch
            {
                if (File.Exists(targetPath)) File.Delete(targetPath);
                if (File.Exists(backupPath)) File.Move(backupPath, targetPath);
                throw;
            }
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            if (staged && File.Exists(backupPath)) File.Delete(backupPath);
        }

        if (staged) _responseCache.Invalidate(repositoryPath, path);
    }

    private static async Task<byte[]?> SelectResultAsync(
        LovelyGitRepository repository,
        IReadOnlyDictionary<int, GitIndexEntry> entries,
        string? resultText,
        ConflictResolutionSource? source,
        bool deleteResult,
        CancellationToken cancellationToken)
    {
        var selectionCount = (deleteResult ? 1 : 0) + (source.HasValue ? 1 : 0) + (resultText != null ? 1 : 0);
        if (selectionCount != 1)
        {
            throw new InvalidOperationException("Choose exactly one conflict result.");
        }

        if (deleteResult)
        {
            return null;
        }

        if (resultText != null)
        {
            if (ContainsConflictMarkers(resultText))
            {
                throw new InvalidOperationException("Resolve every conflict marker before marking the file resolved.");
            }

            return Encoding.UTF8.GetBytes(resultText);
        }

        var stage = source switch
        {
            ConflictResolutionSource.Base => 1,
            ConflictResolutionSource.Ours => 2,
            ConflictResolutionSource.Theirs => 3,
            _ => throw new InvalidOperationException("A conflict result is required."),
        };
        return entries.TryGetValue(stage, out var entry)
            ? await repository.ReadBlobAsync(entry.ObjectId, cancellationToken).ConfigureAwait(false)
            : null;
    }

    internal static bool ContainsConflictMarkers(ReadOnlySpan<char> text)
    {
        while (!text.IsEmpty)
        {
            var lineEnd = text.IndexOf('\n');
            var line = lineEnd < 0 ? text : text[..lineEnd];
            if (line.StartsWith("<<<<<<<", StringComparison.Ordinal)
                || line.StartsWith(">>>>>>>", StringComparison.Ordinal))
            {
                return true;
            }

            if (lineEnd < 0) break;
            text = text[(lineEnd + 1)..];
        }

        return false;
    }
}
