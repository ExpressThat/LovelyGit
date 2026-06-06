using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed class CommitFileDiffCacheRepository
{
    private const int MaxCachedLineLength = 12000;
    private readonly GitRepoCacheDbContext _gitRepoCache;

    public CommitFileDiffCacheRepository(GitRepoCacheDbContext gitRepoCache)
    {
        _gitRepoCache = gitRepoCache;
    }

    public async IAsyncEnumerable<CommitFileDiffCacheEntry> GetCommitFileDiffEntriesAsync(Guid repositoryId)
    {
        await foreach (var entry in _gitRepoCache.CommitFileDiffs.FindAllAsync()
            .ConfigureAwait(false))
        {
            if (entry.RepositoryId == repositoryId)
            {
                yield return entry;
            }
        }
    }

    public async IAsyncEnumerable<CommitFileDiffLineCacheEntry> GetCommitFileDiffLineEntriesAsync(Guid repositoryId)
    {
        await foreach (var entry in _gitRepoCache.CommitFileDiffLines.FindAllAsync()
            .ConfigureAwait(false))
        {
            if (entry.RepositoryId == repositoryId)
            {
                yield return entry;
            }
        }
    }

    public async Task<CommitFileDiffResponse?> GetCommitFileDiffAsync(
        Guid repositoryId,
        string hash,
        string path,
        CommitDiffViewMode viewMode,
        CancellationToken cancellationToken)
    {
        var id = MakeDiffId(repositoryId, hash, path, viewMode);
        var entry = await _gitRepoCache.CommitFileDiffs
            .FindByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (entry == null)
        {
            return null;
        }

        var lines = new List<CommitFileDiffLineCacheEntry>();
        await foreach (var lineEntry in GetCommitFileDiffLineEntriesAsync(repositoryId).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(lineEntry.Hash, hash, StringComparison.Ordinal)
                && string.Equals(lineEntry.Path, path, StringComparison.Ordinal)
                && string.Equals(lineEntry.ViewMode, viewMode.ToString(), StringComparison.Ordinal))
            {
                lines.Add(lineEntry);
            }
        }

        return ToResponse(entry, lines);
    }

    public async Task SaveCommitFileDiffAsync(
        Guid repositoryId,
        string hash,
        string path,
        CommitFileDiffResponse response,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(response);
        var id = MakeDiffId(repositoryId, hash, path, normalized.ViewMode);
        var entry = new CommitFileDiffCacheEntry
        {
            Id = id,
            RepositoryId = repositoryId,
            Hash = hash,
            Path = path,
            ViewMode = normalized.ViewMode.ToString(),
            Status = normalized.Status,
            IsBinary = normalized.IsBinary,
            HasDifferences = normalized.HasDifferences,
        };

        await DeleteLineEntriesAsync(repositoryId, hash, path, normalized.ViewMode, cancellationToken)
            .ConfigureAwait(false);

        if (await _gitRepoCache.CommitFileDiffs.FindByIdAsync(entry.Id, cancellationToken).ConfigureAwait(false) == null)
        {
            await _gitRepoCache.CommitFileDiffs.InsertAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _gitRepoCache.CommitFileDiffs.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        }

        for (var index = 0; index < normalized.Lines.Count; index++)
        {
            var lineEntry = new CommitFileDiffLineCacheEntry
            {
                Id = MakeLineId(repositoryId, hash, path, normalized.ViewMode, index),
                RepositoryId = repositoryId,
                Hash = hash,
                Path = path,
                ViewMode = normalized.ViewMode.ToString(),
                LineIndex = index,
                Line = ToCache(normalized.Lines[index]),
            };
            await _gitRepoCache.CommitFileDiffLines.InsertAsync(lineEntry, cancellationToken).ConfigureAwait(false);
        }

        await _gitRepoCache.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearCommitFileDiffsAsync(
        Guid repositoryId,
        string hash,
        CancellationToken cancellationToken)
    {
        var entries = new List<CommitFileDiffCacheEntry>();
        await foreach (var entry in GetCommitFileDiffEntriesAsync(repositoryId).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(entry.Hash, hash, StringComparison.Ordinal))
            {
                entries.Add(entry);
            }
        }

        var lineEntries = new List<CommitFileDiffLineCacheEntry>();
        await foreach (var entry in GetCommitFileDiffLineEntriesAsync(repositoryId).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(entry.Hash, hash, StringComparison.Ordinal))
            {
                lineEntries.Add(entry);
            }
        }

        foreach (var entry in entries)
        {
            await _gitRepoCache.CommitFileDiffs.DeleteAsync(entry.Id, cancellationToken).ConfigureAwait(false);
        }

        foreach (var entry in lineEntries)
        {
            await _gitRepoCache.CommitFileDiffLines.DeleteAsync(entry.Id, cancellationToken).ConfigureAwait(false);
        }

        await _gitRepoCache.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteLineEntriesAsync(
        Guid repositoryId,
        string hash,
        string path,
        CommitDiffViewMode viewMode,
        CancellationToken cancellationToken)
    {
        var lineEntries = new List<CommitFileDiffLineCacheEntry>();
        await foreach (var entry in GetCommitFileDiffLineEntriesAsync(repositoryId).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(entry.Hash, hash, StringComparison.Ordinal)
                && string.Equals(entry.Path, path, StringComparison.Ordinal)
                && string.Equals(entry.ViewMode, viewMode.ToString(), StringComparison.Ordinal))
            {
                lineEntries.Add(entry);
            }
        }

        foreach (var entry in lineEntries)
        {
            await _gitRepoCache.CommitFileDiffLines.DeleteAsync(entry.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string MakeDiffId(
        Guid repositoryId,
        string hash,
        string path,
        CommitDiffViewMode viewMode)
    {
        return $"{repositoryId:N}:{hash}:{viewMode}:{Uri.EscapeDataString(path)}";
    }

    private static string MakeLineId(
        Guid repositoryId,
        string hash,
        string path,
        CommitDiffViewMode viewMode,
        int index)
    {
        return $"{MakeDiffId(repositoryId, hash, path, viewMode)}:{index}";
    }

    private static CommitFileDiffResponse Normalize(CommitFileDiffResponse response)
    {
        return new CommitFileDiffResponse
        {
            CommitHash = response.CommitHash,
            Path = response.Path,
            Status = response.Status,
            ViewMode = response.ViewMode,
            IsBinary = response.IsBinary,
            HasDifferences = response.HasDifferences,
            Lines = response.Lines.Select(Normalize).ToList(),
        };
    }

    private static CommitFileDiffLine Normalize(CommitFileDiffLine line)
    {
        var oldText = Truncate(line.OldText);
        var newText = Truncate(line.NewText);
        var text = Truncate(line.Text);
        return new CommitFileDiffLine
        {
            OldLineNumber = line.OldLineNumber,
            NewLineNumber = line.NewLineNumber,
            OldText = oldText,
            NewText = newText,
            Text = text,
            ChangeType = line.ChangeType,
            OldSyntaxSpans = TrimSpans(line.OldSyntaxSpans, oldText.Length),
            NewSyntaxSpans = TrimSpans(line.NewSyntaxSpans, newText.Length),
            SyntaxSpans = TrimSpans(line.SyntaxSpans, text.Length),
            OldChangeSpans = TrimSpans(line.OldChangeSpans, oldText.Length),
            NewChangeSpans = TrimSpans(line.NewChangeSpans, newText.Length),
            ChangeSpans = TrimSpans(line.ChangeSpans, text.Length),
        };
    }

    private static string Truncate(string value)
    {
        if (value.Length <= MaxCachedLineLength)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, MaxCachedLineLength), " ... [line truncated]");
    }

    private static List<CommitFileDiffSyntaxSpan> TrimSpans(
        IEnumerable<CommitFileDiffSyntaxSpan> spans,
        int maxLength)
    {
        return spans
            .Where(span => span.Start < maxLength)
            .Select(span => new CommitFileDiffSyntaxSpan
            {
                Start = span.Start,
                Length = Math.Min(span.Length, maxLength - span.Start),
                Scope = span.Scope,
            })
            .Where(span => span.Length > 0)
            .ToList();
    }

    private static List<CommitFileDiffChangeSpan> TrimSpans(
        IEnumerable<CommitFileDiffChangeSpan> spans,
        int maxLength)
    {
        return spans
            .Where(span => span.Start < maxLength)
            .Select(span => new CommitFileDiffChangeSpan
            {
                Start = span.Start,
                Length = Math.Min(span.Length, maxLength - span.Start),
                ChangeType = span.ChangeType,
            })
            .Where(span => span.Length > 0)
            .ToList();
    }

    private static CommitFileDiffLineCache ToCache(CommitFileDiffLine line)
    {
        return new CommitFileDiffLineCache
        {
            OldLineNumber = line.OldLineNumber,
            NewLineNumber = line.NewLineNumber,
            OldText = line.OldText,
            NewText = line.NewText,
            Text = line.Text,
            ChangeType = line.ChangeType,
            OldSyntaxSpans = ToCache(line.OldSyntaxSpans),
            NewSyntaxSpans = ToCache(line.NewSyntaxSpans),
            SyntaxSpans = ToCache(line.SyntaxSpans),
            OldChangeSpans = ToCache(line.OldChangeSpans),
            NewChangeSpans = ToCache(line.NewChangeSpans),
            ChangeSpans = ToCache(line.ChangeSpans),
        };
    }

    private static CommitFileDiffResponse ToResponse(
        CommitFileDiffCacheEntry cache,
        IEnumerable<CommitFileDiffLineCacheEntry> lines)
    {
        return new CommitFileDiffResponse
        {
            CommitHash = cache.Hash,
            Path = cache.Path,
            Status = cache.Status,
            ViewMode = Enum.TryParse<CommitDiffViewMode>(cache.ViewMode, out var viewMode)
                ? viewMode
                : CommitDiffViewMode.SideBySide,
            IsBinary = cache.IsBinary,
            HasDifferences = cache.HasDifferences,
            Lines = lines
                .OrderBy(line => line.LineIndex)
                .Select(line => line.Line)
                .Select(ToResponse)
                .ToList(),
        };
    }

    private static CommitFileDiffLine ToResponse(CommitFileDiffLineCache line)
    {
        return new CommitFileDiffLine
        {
            OldLineNumber = line.OldLineNumber,
            NewLineNumber = line.NewLineNumber,
            OldText = line.OldText,
            NewText = line.NewText,
            Text = line.Text,
            ChangeType = line.ChangeType,
            OldSyntaxSpans = ToResponse(line.OldSyntaxSpans),
            NewSyntaxSpans = ToResponse(line.NewSyntaxSpans),
            SyntaxSpans = ToResponse(line.SyntaxSpans),
            OldChangeSpans = ToResponse(line.OldChangeSpans),
            NewChangeSpans = ToResponse(line.NewChangeSpans),
            ChangeSpans = ToResponse(line.ChangeSpans),
        };
    }

    private static List<CommitFileDiffSyntaxSpanCache> ToCache(IEnumerable<CommitFileDiffSyntaxSpan> spans)
    {
        return spans.Select(span => new CommitFileDiffSyntaxSpanCache
        {
            Start = span.Start,
            Length = span.Length,
            Scope = span.Scope,
        }).ToList();
    }

    private static List<CommitFileDiffSyntaxSpan> ToResponse(IEnumerable<CommitFileDiffSyntaxSpanCache> spans)
    {
        return spans.Select(span => new CommitFileDiffSyntaxSpan
        {
            Start = span.Start,
            Length = span.Length,
            Scope = span.Scope,
        }).ToList();
    }

    private static List<CommitFileDiffChangeSpanCache> ToCache(IEnumerable<CommitFileDiffChangeSpan> spans)
    {
        return spans.Select(span => new CommitFileDiffChangeSpanCache
        {
            Start = span.Start,
            Length = span.Length,
            ChangeType = span.ChangeType,
        }).ToList();
    }

    private static List<CommitFileDiffChangeSpan> ToResponse(IEnumerable<CommitFileDiffChangeSpanCache> spans)
    {
        return spans.Select(span => new CommitFileDiffChangeSpan
        {
            Start = span.Start,
            Length = span.Length,
            ChangeType = span.ChangeType,
        }).ToList();
    }
}
