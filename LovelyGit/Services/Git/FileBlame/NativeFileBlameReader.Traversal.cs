using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.FileBlame;

internal static partial class NativeFileBlameReader
{
    private static async Task<BlameTraversalResult> TraceAsync(
        LovelyGitRepository repository,
        BlameWorkItem first,
        GitCommit?[] attributions,
        int maximumCommits,
        TimeSpan maximumDuration,
        CancellationToken cancellationToken)
    {
        var pending = new Queue<BlameWorkItem>();
        pending.Enqueue(first);
        var startedAt = Stopwatch.GetTimestamp();
        var scanned = 0;
        while (pending.Count > 0 && scanned < Math.Max(1, maximumCommits))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((scanned & 31) == 0 && maximumDuration != Timeout.InfiniteTimeSpan
                && Stopwatch.GetElapsedTime(startedAt) >= maximumDuration)
            {
                break;
            }

            var work = pending.Dequeue();
            scanned++;
            if (work.State.Header.ParentHashCount == 0)
            {
                await AttributeAsync(
                        repository, work.State.Hash, work.ActiveLines, attributions, cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            await DistributeToParentsAsync(
                    repository, work, pending, attributions, cancellationToken)
                .ConfigureAwait(false);
        }

        return new BlameTraversalResult(scanned, pending.Count > 0);
    }

    private static async Task DistributeToParentsAsync(
        LovelyGitRepository repository,
        BlameWorkItem work,
        Queue<BlameWorkItem> pending,
        GitCommit?[] attributions,
        CancellationToken cancellationToken)
    {
        var remaining = work.ActiveLines;
        for (var index = 0; index < work.State.Header.ParentHashCount && remaining.Count > 0; index++)
        {
            var parent = await LoadParentAsync(
                    repository, work.State, work.State.Header.GetParentHash(index), cancellationToken)
                .ConfigureAwait(false);
            if (parent == null)
            {
                continue;
            }

            if (work.State.File.ObjectId == parent.Value.File.ObjectId)
            {
                pending.Enqueue(new BlameWorkItem(parent.Value, remaining));
                return;
            }

            var carried = CarryUnchangedLines(work.State, parent.Value, remaining);
            if (carried.Count > 0)
            {
                pending.Enqueue(new BlameWorkItem(parent.Value, carried));
            }
        }

        if (remaining.Count > 0)
        {
            await AttributeAsync(
                    repository, work.State.Hash, remaining, attributions, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static List<ActiveLine> CarryUnchangedLines(
        BlameState current,
        BlameState parent,
        List<ActiveLine> remaining)
    {
        var mapping = BlameLineMapper.MapNewLinesToOld(
            parent.Text.Content, current.Text.Content, current.Text.LineCount);
        var carried = new List<ActiveLine>(remaining.Count);
        var remainingCount = 0;
        for (var index = 0; index < remaining.Count; index++)
        {
            var line = remaining[index];
            var parentLine = line.CurrentLine < mapping.Length ? mapping[line.CurrentLine] : -1;
            if (parentLine < 0)
            {
                remaining[remainingCount++] = line;
                continue;
            }

            carried.Add(line with { CurrentLine = parentLine });
        }

        if (remainingCount < remaining.Count)
        {
            remaining.RemoveRange(remainingCount, remaining.Count - remainingCount);
        }

        return carried;
    }

    private static async Task<BlameState?> LoadParentAsync(
        LovelyGitRepository repository,
        BlameState current,
        GitObjectId parentHash,
        CancellationToken cancellationToken)
    {
        var header = await repository.GetCommitAncestryHeaderAsync(parentHash, cancellationToken)
            .ConfigureAwait(false);
        var file = await FindFileAsync(repository, header, current.Path, cancellationToken)
            .ConfigureAwait(false);
        var path = current.Path;
        if (file == null && header.TreeHash != null)
        {
            file = await repository.FindTreeFileByObjectIdAsync(
                    header.TreeHash.Value, current.File.ObjectId, cancellationToken)
                .ConfigureAwait(false);
            path = file?.Path ?? path;
        }

        if (file == null)
        {
            return null;
        }

        var text = file.ObjectId == current.File.ObjectId
            ? current.Text
            : BlameText.Decode(
                await repository.ReadBlobAsync(file.ObjectId, cancellationToken).ConfigureAwait(false));
        return new BlameState(parentHash, path, header, file, text);
    }

    private static async Task AttributeAsync(
        LovelyGitRepository repository,
        GitObjectId hash,
        IEnumerable<ActiveLine> lines,
        GitCommit?[] attributions,
        CancellationToken cancellationToken)
    {
        var commit = await repository.GetCommitAsync(hash, cancellationToken).ConfigureAwait(false);
        foreach (var line in lines)
        {
            attributions[line.OriginalLine] = commit;
        }
    }
}
