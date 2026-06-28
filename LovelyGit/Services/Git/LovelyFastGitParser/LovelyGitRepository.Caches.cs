namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository
{
    public void ClearObjectCaches()
    {
        _commitCache.Clear();
        _objectStore.ClearObjectCaches();
    }
}
