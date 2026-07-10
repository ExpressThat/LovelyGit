namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository
{
    public async Task<GitCommitSearchHeader> GetCommitSearchHeaderAsync(
        GitObjectId id,
        ReadOnlyMemory<byte> queryUtf8,
        string query,
        CancellationToken cancellationToken)
    {
        var data = await _objectStore.ReadObjectAsync(id, cancellationToken).ConfigureAwait(false);
        if (data.Kind != GitObjectKind.Commit)
        {
            throw new InvalidDataException($"Object is not a commit: {id}");
        }

        return GitObjectParsers.ParseCommitSearchHeader(id, data.Data, queryUtf8.Span, query);
    }
}
