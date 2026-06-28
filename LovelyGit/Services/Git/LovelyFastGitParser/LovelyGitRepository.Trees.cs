using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository : IDisposable
{
    public async Task<List<GitTreeEntry>> ReadRootTreeEntriesAsync(
        GitObjectId treeId,
        CancellationToken cancellationToken) =>
        await ReadTreeEntriesAsync(treeId, string.Empty, cancellationToken).ConfigureAwait(false);

    private async Task ReadTreeFilesAsync(
        GitObjectId treeId,
        string prefix,
        Dictionary<string, GitTreeFile> files,
        CancellationToken cancellationToken)
    {
        foreach (var entry in await ReadTreeEntriesAsync(treeId, prefix, cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.IsTree)
            {
                await ReadTreeFilesAsync(entry.ObjectId, entry.Path, files, cancellationToken).ConfigureAwait(false);
                continue;
            }

            files[entry.Path] = new GitTreeFile(entry.Path, entry.ObjectId, entry.Mode);
        }
    }

    private async Task CompareTreesAsync(
        GitObjectId? parentTreeId,
        GitObjectId? currentTreeId,
        string prefix,
        Dictionary<string, GitTreeFile> parentFiles,
        Dictionary<string, GitTreeFile> currentFiles,
        CancellationToken cancellationToken)
    {
        if (parentTreeId == currentTreeId)
        {
            return;
        }

        var parentEntries = parentTreeId == null
            ? new Dictionary<string, GitTreeEntry>(StringComparer.Ordinal)
            : (await ReadTreeEntriesAsync(parentTreeId.Value, prefix, cancellationToken).ConfigureAwait(false))
                .ToDictionary(entry => entry.Name, StringComparer.Ordinal);
        var currentEntries = currentTreeId == null
            ? new Dictionary<string, GitTreeEntry>(StringComparer.Ordinal)
            : (await ReadTreeEntriesAsync(currentTreeId.Value, prefix, cancellationToken).ConfigureAwait(false))
                .ToDictionary(entry => entry.Name, StringComparer.Ordinal);
        var names = parentEntries.Keys
            .Concat(currentEntries.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal);

        foreach (var name in names)
        {
            cancellationToken.ThrowIfCancellationRequested();
            parentEntries.TryGetValue(name, out var parentEntry);
            currentEntries.TryGetValue(name, out var currentEntry);

            if (parentEntry?.ObjectId == currentEntry?.ObjectId && parentEntry?.Mode == currentEntry?.Mode)
            {
                continue;
            }

            if (parentEntry?.IsTree == true && currentEntry?.IsTree == true)
            {
                await CompareTreesAsync(
                        parentEntry.ObjectId,
                        currentEntry.ObjectId,
                        parentEntry.Path,
                        parentFiles,
                        currentFiles,
                        cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            if (parentEntry != null)
            {
                await CollectTreeEntryFilesAsync(parentEntry, parentFiles, cancellationToken).ConfigureAwait(false);
            }

            if (currentEntry != null)
            {
                await CollectTreeEntryFilesAsync(currentEntry, currentFiles, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task CollectTreeEntryFilesAsync(
        GitTreeEntry entry,
        Dictionary<string, GitTreeFile> files,
        CancellationToken cancellationToken)
    {
        if (!entry.IsTree)
        {
            files[entry.Path] = new GitTreeFile(entry.Path, entry.ObjectId, entry.Mode);
            return;
        }

        await ReadTreeFilesAsync(entry.ObjectId, entry.Path, files, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<GitTreeEntry>> ReadTreeEntriesAsync(
        GitObjectId treeId,
        string prefix,
        CancellationToken cancellationToken)
    {
        var data = await _objectStore.ReadObjectAsync(treeId, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return GitObjectParsers.ParseTreeEntries(treeId, ObjectFormat, data, prefix);
    }

}
