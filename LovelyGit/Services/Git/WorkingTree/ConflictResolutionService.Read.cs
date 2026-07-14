using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionService
{
    private const int MaxTextBytes = 4 * 1024 * 1024;

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
        readTrace.Mark("repository-paths");
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        readTrace.Mark("repository-open");
        var entries = await ReadConflictEntriesAsync(repository, path, cancellationToken).ConfigureAwait(false);
        readTrace.Mark("index-entry");
        var resultPath = WorkingTreePath.Resolve(repository.WorkTreeDirectory, path);
        var resultSnapshot = await ConflictWorktreeSnapshotReader
            .ReadAsync(resultPath, MaxTextBytes, cancellationToken)
            .ConfigureAwait(false);
        var fingerprint = ConflictFingerprint(resultSnapshot.Fingerprint, entries);
        readTrace.Mark("fingerprint");
        var cacheStamp = ConflictResolutionCacheStamp.Capture(
            Path.Combine(repository.GitDirectory, "index"),
            resultPath,
            resultSnapshot.Stamp);
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
        var result = CreateWorktreeVersion(resultSnapshot);
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

        var hunks = canBuildTextMerge
            ? ConflictHunkBuilder.Build(
                result.Text!,
                diffModels!.CurrentHunk,
                diffModels.IncomingHunk)
            : [];
        readTrace.Mark("hunks");
        var currentComparison = canBuildTextMerge
            ? BuildBaseComparison(path, baseVersion.Text!, ours.Text!, diffModels!.CurrentComparison)
            : null;
        readTrace.Mark("current-comparison");
        var incomingComparison = canBuildTextMerge
            ? BuildBaseComparison(path, baseVersion.Text!, theirs.Text!, diffModels!.IncomingComparison)
            : null;
        readTrace.Mark("incoming-comparison");
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
            Hunks = hunks,
            CurrentComparison = currentComparison,
            IncomingComparison = incomingComparison,
        };
        var retainedSources = ConflictTextPayloadBuilder.RetainSources(response);
        readTrace.Mark("retain-sources");
        ConflictComparisonPayloadBuilder.Compact(response.CurrentComparison);
        ConflictComparisonPayloadBuilder.Compact(response.IncomingComparison);
        readTrace.Mark("compact-comparisons");
        ConflictTextPayloadBuilder.Compact(response);
        readTrace.Mark("compact-text");
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
            .ReadEntriesForPathAsync(repository.GitDirectory, repository.ObjectFormat, path, cancellationToken)
            .ConfigureAwait(false);
        var conflict = entries
            .Where(entry => entry.Stage is >= 1 and <= 3)
            .ToDictionary(entry => entry.Stage);
        if (conflict.Count == 0)
        {
            throw new InvalidOperationException("This file no longer has an unresolved conflict.");
        }

        return conflict;
    }

}
