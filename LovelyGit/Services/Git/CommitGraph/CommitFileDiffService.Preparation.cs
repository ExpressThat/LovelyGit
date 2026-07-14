using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
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
                await ClearPersistentDiffsAsync(
                        repositoryId,
                        previousCommitHash,
                        CancellationToken.None)
                    .ConfigureAwait(false);
            }

            foreach (var file in CommitFileDiffPreparationPolicy.SelectFiles(changedFiles))
            {
                cancellationToken.ThrowIfCancellationRequested();

                await SaveMissingViewModesAsync(
                        repositoryId,
                        repositoryPath,
                        commitHash,
                        file.Path,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
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

    private async Task<CommitFileDiffResponse> BuildAndCacheMissingDiffAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        var gateKey = MakeDiffGateKey(repositoryId, commitHash, path, viewMode, ignoreWhitespace);
        var gate = GetBuildGate(gateKey);
        var enteredGate = false;
        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredGate = true;

            var cached = await TryGetCachedDiffAsync(
                    repositoryId,
                    commitHash,
                    path,
                    viewMode,
                    ignoreWhitespace,
                    cancellationToken)
                .ConfigureAwait(false);
            if (cached != null)
            {
                return cached;
            }

            var response = await BuildCommitFileDiffAsync(
                repositoryPath,
                commitHash,
                null,
                0,
                    path,
                    viewMode,
                    ignoreWhitespace,
                    cancellationToken)
                .ConfigureAwait(false);

            if (CommitFileDiffCachingPolicy.ShouldPersist(response))
            {
                QueueDiffPersistence(
                    repositoryId,
                    commitHash,
                    path,
                    response,
                    ignoreWhitespace,
                    cancellationToken);
            }

            return response;
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
        const bool ignoreWhitespace = false;
        var gateKey = MakeDiffGateKey(repositoryId, commitHash, path, viewMode, ignoreWhitespace);
        var gate = GetBuildGate(gateKey);
        var enteredGate = false;
        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredGate = true;

            var cached = await TryGetCachedDiffAsync(
                    repositoryId,
                    commitHash,
                    path,
                    viewMode,
                    ignoreWhitespace,
                    cancellationToken)
                .ConfigureAwait(false);
            if (cached != null)
            {
                return cached;
            }

            var response = BuildResponseFromSource(commitHash, path, viewMode, ignoreWhitespace, source);
            if (CommitFileDiffCachingPolicy.ShouldPersist(response))
            {
                await SavePersistentDiffAsync(
                        repositoryId,
                        commitHash,
                        path,
                        response,
                        ignoreWhitespace,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return response;
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
