using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService
{
    private async Task BuildAndCacheMissingDiffPairAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitFileDiffSource source,
        CancellationToken cancellationToken)
    {
        const bool ignoreWhitespace = false;
        var sideKey = MakeDiffGateKey(
            repositoryId, commitHash, path, CommitDiffViewMode.SideBySide, ignoreWhitespace);
        var combinedKey = MakeDiffGateKey(
            repositoryId, commitHash, path, CommitDiffViewMode.Combined, ignoreWhitespace);
        var sideGate = GetBuildGate(sideKey);
        var combinedGate = GetBuildGate(combinedKey);
        var sideEntered = false;
        var combinedEntered = false;
        try
        {
            await sideGate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            sideEntered = true;
            await combinedGate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            combinedEntered = true;

            var side = await TryGetCachedDiffAsync(
                    repositoryId,
                    commitHash,
                    path,
                    CommitDiffViewMode.SideBySide,
                    ignoreWhitespace,
                    cancellationToken)
                .ConfigureAwait(false);
            var combined = await TryGetCachedDiffAsync(
                    repositoryId,
                    commitHash,
                    path,
                    CommitDiffViewMode.Combined,
                    ignoreWhitespace,
                    cancellationToken)
                .ConfigureAwait(false);
            if (side != null && combined != null) return;

            if (side == null && combined == null)
            {
                (side, combined) = BuildResponsePairFromSource(commitHash, path, source);
            }
            else if (side == null)
            {
                side = BuildResponseFromSource(
                    commitHash, path, CommitDiffViewMode.SideBySide, ignoreWhitespace, source);
            }
            else
            {
                combined = BuildResponseFromSource(
                    commitHash, path, CommitDiffViewMode.Combined, ignoreWhitespace, source);
            }

            await SavePreparedResponseAsync(repositoryId, commitHash, path, side!, cancellationToken)
                .ConfigureAwait(false);
            await SavePreparedResponseAsync(repositoryId, commitHash, path, combined!, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            if (combinedEntered) combinedGate.Semaphore.Release();
            if (sideEntered) sideGate.Semaphore.Release();
            ReleaseBuildGate(combinedKey, combinedGate);
            ReleaseBuildGate(sideKey, sideGate);
        }
    }

    private async Task SavePreparedResponseAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitFileDiffResponse response,
        CancellationToken cancellationToken)
    {
        if (!CommitFileDiffCachingPolicy.ShouldPersist(response)) return;
        await _commitGraphRepository
            .SaveCommitFileDiffAsync(
                repositoryId,
                commitHash,
                path,
                response,
                ignoreWhitespace: false,
                cancellationToken)
            .ConfigureAwait(false);
    }

    internal static (CommitFileDiffResponse SideBySide, CommitFileDiffResponse Combined)
        BuildResponsePairFromSource(
            string commitHash,
            string path,
            CommitFileDiffSource source)
    {
        if (source.IsBinary
            || DiffInputGuard.ShouldUseVirtualText(source.OldText, source.NewText)
            || DiffInputGuard.ShouldUseFastDiff(source.OldText, source.NewText))
        {
            var side = BuildResponseFromSource(
                commitHash, path, CommitDiffViewMode.SideBySide, false, source);
            return (side, side with { ViewMode = CommitDiffViewMode.Combined });
        }

        var uncompressedSide = BuildSideBySideResponse(
            commitHash,
            path,
            source.Status,
            source.OldText,
            source.NewText,
            source.Language,
            ignoreWhitespace: false);
        var uncompressedCombined = BuildCombinedFromSideBySide(uncompressedSide);
        return (
            CompactDiffPayloadBuilder.CompactIfUseful(uncompressedSide),
            CompactDiffPayloadBuilder.CompactIfUseful(uncompressedCombined));
    }

    private static CommitFileDiffResponse BuildCombinedFromSideBySide(
        CommitFileDiffResponse side)
    {
        var lines = new List<CommitFileDiffLine>(side.Lines.Count * 2);
        foreach (var line in side.Lines)
        {
            if (line.ChangeType == "Unchanged")
            {
                lines.Add(new CommitFileDiffLine
                {
                    OldLineNumber = line.OldLineNumber,
                    NewLineNumber = line.NewLineNumber,
                    Text = line.OldText,
                    ChangeType = "Unchanged",
                    SyntaxSpans = line.OldSyntaxSpans,
                });
                continue;
            }

            if (line.OldLineNumber.HasValue)
            {
                lines.Add(new CommitFileDiffLine
                {
                    OldLineNumber = line.OldLineNumber,
                    Text = line.OldText,
                    ChangeType = "Deleted",
                    SyntaxSpans = line.OldSyntaxSpans,
                    ChangeSpans = line.OldChangeSpans,
                });
            }

            if (line.NewLineNumber.HasValue)
            {
                lines.Add(new CommitFileDiffLine
                {
                    NewLineNumber = line.NewLineNumber,
                    Text = line.NewText,
                    ChangeType = "Inserted",
                    SyntaxSpans = line.NewSyntaxSpans,
                    ChangeSpans = line.NewChangeSpans,
                });
            }
        }

        return side with
        {
            ViewMode = CommitDiffViewMode.Combined,
            Lines = lines,
        };
    }
}
