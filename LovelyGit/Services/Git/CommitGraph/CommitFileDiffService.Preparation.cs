using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService : IDisposable
{
    private async Task PrepareCommitDiffsAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        IReadOnlyList<CommitChangedFile> changedFiles,
        string? previousCommitHash,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(previousCommitHash))
            {
                await _commitGraphRepository
                    .ClearCommitFileDiffsAsync(repositoryId, previousCommitHash, CancellationToken.None)
                    .ConfigureAwait(false);
            }

            await Parallel.ForEachAsync(changedFiles, cancellationToken, async (file, fileCancellationToken) =>
            {
                fileCancellationToken.ThrowIfCancellationRequested();

                await SaveMissingViewModesAsync(
                        repositoryId,
                        repositoryPath,
                        commitHash,
                        file.Path,
                        fileCancellationToken)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
        finally
        {
            lock (_preparationLock)
            {
                if (_activePreparations.TryGetValue(repositoryId, out var active)
                    && string.Equals(active.CommitHash, commitHash, StringComparison.Ordinal))
                {
                    _activePreparations.Remove(repositoryId);
                    active.Dispose();
                }
            }

        }
    }

    private async Task SaveMissingViewModesAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        string path,
        CancellationToken cancellationToken)
    {
        var hasSideBySide = await _commitGraphRepository
            .HasCommitFileDiffAsync(repositoryId, commitHash, path, CommitDiffViewMode.SideBySide, cancellationToken)
            .ConfigureAwait(false);
        var hasCombined = await _commitGraphRepository
            .HasCommitFileDiffAsync(repositoryId, commitHash, path, CommitDiffViewMode.Combined, cancellationToken)
            .ConfigureAwait(false);

        if (hasSideBySide && hasCombined)
        {
            return;
        }

        var source = await BuildCommitFileDiffSourceAsync(repositoryPath, commitHash, path, cancellationToken)
            .ConfigureAwait(false);

        if (!hasSideBySide)
        {
            await BuildAndCacheMissingDiffAsync(
                    repositoryId,
                    repositoryPath,
                    commitHash,
                    path,
                    CommitDiffViewMode.SideBySide,
                    source,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (!hasCombined)
        {
            await BuildAndCacheMissingDiffAsync(
                    repositoryId,
                    repositoryPath,
                    commitHash,
                    path,
                    CommitDiffViewMode.Combined,
                    source,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<CommitFileDiffResponse> BuildAndCacheMissingDiffAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        CancellationToken cancellationToken)
    {
        var gateKey = MakeDiffGateKey(repositoryId, commitHash, path, viewMode);
        var gate = GetBuildGate(gateKey);
        var enteredGate = false;
        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredGate = true;

            var cached = await TryGetCachedDiffAsync(repositoryId, commitHash, path, viewMode, cancellationToken)
                .ConfigureAwait(false);
            if (cached != null)
            {
                return cached;
            }

            var response = await BuildCommitFileDiffAsync(
                    repositoryPath,
                    commitHash,
                    path,
                    viewMode,
                    cancellationToken)
                .ConfigureAwait(false);

            await _commitGraphRepository
                .SaveCommitFileDiffAsync(repositoryId, commitHash, path, response, cancellationToken)
                .ConfigureAwait(false);

            return await TryGetCachedDiffAsync(repositoryId, commitHash, path, viewMode, cancellationToken)
                .ConfigureAwait(false) ?? response;
        }
        finally
        {
            if (enteredGate)
            {
                gate.Semaphore.Release();
            }

            ReleaseBuildGate(gateKey, gate);
        }
    }

    private async Task<CommitFileDiffResponse> BuildAndCacheMissingDiffAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        CommitFileDiffSource source,
        CancellationToken cancellationToken)
    {
        var gateKey = MakeDiffGateKey(repositoryId, commitHash, path, viewMode);
        var gate = GetBuildGate(gateKey);
        var enteredGate = false;
        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredGate = true;

            var cached = await TryGetCachedDiffAsync(repositoryId, commitHash, path, viewMode, cancellationToken)
                .ConfigureAwait(false);
            if (cached != null)
            {
                return cached;
            }

            var response = BuildResponseFromSource(commitHash, path, viewMode, source);
            await _commitGraphRepository
                .SaveCommitFileDiffAsync(repositoryId, commitHash, path, response, cancellationToken)
                .ConfigureAwait(false);

            return await TryGetCachedDiffAsync(repositoryId, commitHash, path, viewMode, cancellationToken)
                .ConfigureAwait(false) ?? response;
        }
        finally
        {
            if (enteredGate)
            {
                gate.Semaphore.Release();
            }

            ReleaseBuildGate(gateKey, gate);
        }
    }

}
