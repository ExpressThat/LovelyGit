namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class GitIgnoreMatcher
{
    public bool SourcesAreCurrent()
    {
        foreach (var source in _sourceStamps)
        {
            if (source != GitIgnoreSourceStamp.Read(source.Path))
            {
                return false;
            }
        }

        return true;
    }
}

internal readonly record struct GitIgnoreSourceStamp(
    string Path,
    bool Exists,
    long Length,
    long LastWriteTicks)
{
    public static GitIgnoreSourceStamp Read(string path)
    {
        var info = new FileInfo(path);
        return info.Exists
            ? new GitIgnoreSourceStamp(path, true, info.Length, info.LastWriteTimeUtc.Ticks)
            : new GitIgnoreSourceStamp(path, false, 0, 0);
    }
}
