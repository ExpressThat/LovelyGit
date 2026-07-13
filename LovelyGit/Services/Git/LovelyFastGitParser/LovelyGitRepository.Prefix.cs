namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository
{
    public Task<GitObjectId?> ResolveUniqueObjectPrefixAsync(
        string prefix,
        CancellationToken cancellationToken) =>
        _objectStore.ResolveUniquePrefixAsync(prefix, cancellationToken);
}
