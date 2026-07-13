using System.Text;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionService
{
    private const int MaxTextBytes = 4 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public async Task<ConflictResolutionResponse> ReadAsync(
        string repositoryPath,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        using var readTrace = ConflictReadTrace.Start(path, ignoreWhitespace);
        cancellationToken.ThrowIfCancellationRequested();
        path = WorkingTreePath.NormalizeRelative(path);
        if (TryReadMetadataCached(
                repositoryPath,
                path,
                ignoreWhitespace,
                readTrace,
                out var current))
        {
            return current;
        }

        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var entries = await ReadConflictEntriesAsync(repository, path, cancellationToken).ConfigureAwait(false);
        var resultPath = WorkingTreePath.Resolve(repository.WorkTreeDirectory, path);
        var fingerprint = await ConflictFingerprintAsync(resultPath, entries, cancellationToken)
            .ConfigureAwait(false);
        var cacheStamp = ConflictResolutionCacheStamp.Capture(
            Path.Combine(repository.GitDirectory, "index"),
            resultPath);
        readTrace.Mark("repository-and-fingerprint");
        _responseCache.RemoveStale(repositoryPath, path, fingerprint);
        if (_responseCache.TryGet(
                repositoryPath,
                path,
                fingerprint,
                ignoreWhitespace,
                out var cached))
        {
            return cached;
        }

        if (_responseCache.TryGetSibling(
                repositoryPath,
                path,
                fingerprint,
                ignoreWhitespace,
                out var sibling,
                out var retainedTexts))
        {
            var variant = BuildCachedVariant(sibling, retainedTexts, ignoreWhitespace);
            readTrace.Mark("sibling-variant");
            _responseCache.Set(
                repositoryPath,
                path,
                fingerprint,
                ignoreWhitespace,
                variant,
                cacheStamp,
                retainedTexts);
            return variant;
        }

        var baseVersion = await ReadVersionAsync(repository, entries.GetValueOrDefault(1), cancellationToken)
            .ConfigureAwait(false);
        var ours = await ReadVersionAsync(repository, entries.GetValueOrDefault(2), cancellationToken)
            .ConfigureAwait(false);
        var theirs = await ReadVersionAsync(repository, entries.GetValueOrDefault(3), cancellationToken)
            .ConfigureAwait(false);
        var result = await ReadWorktreeVersionAsync(resultPath, cancellationToken).ConfigureAwait(false);
        readTrace.Mark("read-versions");
        var currentSource = CreateCurrentSource(repository, entries.GetValueOrDefault(2));
        var incomingSource = await CreateIncomingSourceAsync(
            repository,
            repositoryPaths.WorktreeGitDirectory,
            entries.GetValueOrDefault(3),
            cancellationToken).ConfigureAwait(false);
        readTrace.Mark("source-metadata");
        var canBuildTextMerge = baseVersion.Text != null &&
            ours.Text != null &&
            theirs.Text != null &&
            result.Text != null;
        var diffModels = canBuildTextMerge
            ? PrepareDiffModels(baseVersion.Text!, ours.Text!, theirs.Text!, ignoreWhitespace)
            : null;
        readTrace.Mark("diff-models");

        var response = new ConflictResolutionResponse
        {
            Path = path,
            WorktreeFingerprint = fingerprint,
            Base = baseVersion,
            Ours = ours,
            Theirs = theirs,
            Result = result,
            CurrentSource = currentSource,
            IncomingSource = incomingSource,
            Hunks = canBuildTextMerge
                ? ConflictHunkBuilder.Build(
                    result.Text!,
                    diffModels!.CurrentHunk,
                    diffModels.IncomingHunk)
                : new List<ConflictHunk>(),
            CurrentComparison = canBuildTextMerge
                ? BuildBaseComparison(path, baseVersion.Text!, ours.Text!, diffModels!.CurrentComparison)
                : null,
            IncomingComparison = canBuildTextMerge
                ? BuildBaseComparison(path, baseVersion.Text!, theirs.Text!, diffModels!.IncomingComparison)
                : null,
        };
        readTrace.Mark("hunks-and-rendering");
        var retainedSources = ConflictTextPayloadBuilder.RetainSources(response);
        ConflictComparisonPayloadBuilder.Compact(response.CurrentComparison);
        ConflictComparisonPayloadBuilder.Compact(response.IncomingComparison);
        ConflictTextPayloadBuilder.Compact(response);
        readTrace.Mark("payload-compaction");
        _responseCache.Set(
            repositoryPath,
            path,
            fingerprint,
            ignoreWhitespace,
            response,
            cacheStamp,
            retainedSources);
        return response;
    }

    private static CommitFileDiffResponse BuildBaseComparison(
        string path,
        string baseText,
        string sourceText,
        LineDiffModel model)
    {
        return WorkingTreeChangeService.BuildPreparedLineDiffResponse(
            "CONFLICT", path, "Unmerged", baseText, sourceText, model);
    }

    private static async Task<Dictionary<int, GitIndexEntry>> ReadConflictEntriesAsync(
        LovelyGitRepository repository,
        string path,
        CancellationToken cancellationToken)
    {
        var entries = await new GitIndexReader()
            .ReadAsync(repository.GitDirectory, repository.ObjectFormat, cancellationToken)
            .ConfigureAwait(false);
        var conflict = entries
            .Where(entry => entry.Path == path && entry.Stage is >= 1 and <= 3)
            .ToDictionary(entry => entry.Stage);
        if (conflict.Count == 0)
        {
            throw new InvalidOperationException("This file no longer has an unresolved conflict.");
        }

        return conflict;
    }

    private static async Task<ConflictFileVersion> ReadVersionAsync(
        LovelyGitRepository repository,
        GitIndexEntry? entry,
        CancellationToken cancellationToken)
    {
        if (entry == null)
        {
            return new ConflictFileVersion();
        }

        if (entry.FileSize > MaxTextBytes)
        {
            return new ConflictFileVersion { Exists = true, IsTooLarge = true, SizeBytes = entry.FileSize };
        }

        var bytes = await repository
            .ReadBlobWithoutCachingAsync(entry.ObjectId, cancellationToken)
            .ConfigureAwait(false);
        return CreateVersion(bytes);
    }

    private static async Task<ConflictFileVersion> ReadWorktreeVersionAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return new ConflictFileVersion();
        }

        var info = new FileInfo(path);
        if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidOperationException("Symbolic-link conflicts are not supported yet.");
        }

        if (info.Length > MaxTextBytes)
        {
            return new ConflictFileVersion { Exists = true, IsTooLarge = true, SizeBytes = info.Length };
        }

        return CreateVersion(await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false));
    }

    private static ConflictFileVersion CreateVersion(byte[] bytes)
    {
        var isBinary = WorkingTreeChangeService.IsBinary(bytes);
        string? text = null;
        if (!isBinary)
        {
            try
            {
                text = StrictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                isBinary = true;
            }
        }

        return new ConflictFileVersion
        {
            Exists = true,
            IsBinary = isBinary,
            SizeBytes = bytes.Length,
            Text = text,
        };
    }

}
