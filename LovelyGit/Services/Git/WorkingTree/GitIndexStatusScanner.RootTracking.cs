namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class GitIndexStatusScanner
{
    private static void AddRootTracking(
        string path,
        HashSet<string> rootTrackedFiles,
        HashSet<string> rootTrackedDirectories)
    {
        rootTrackedFiles.Add(path);
        var slash = path.IndexOf('/');
        if (slash < 0)
        {
            return;
        }

        while (slash >= 0)
        {
            rootTrackedDirectories.Add(path[..slash]);
            slash = path.IndexOf('/', slash + 1);
        }
    }
}
