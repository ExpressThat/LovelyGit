namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal readonly record struct ConflictResolutionCacheStamp(
    string IndexPath,
    ConflictResolutionFileStamp Index,
    string ResultPath,
    ConflictResolutionFileStamp Result)
{
    public static ConflictResolutionCacheStamp Capture(string indexPath, string resultPath) => new(
        indexPath,
        ConflictResolutionFileStamp.Capture(indexPath),
        resultPath,
        ConflictResolutionFileStamp.Capture(resultPath));

    public static ConflictResolutionCacheStamp Capture(
        string indexPath,
        string resultPath,
        ConflictResolutionFileStamp resultStamp) => new(
        indexPath,
        ConflictResolutionFileStamp.Capture(indexPath),
        resultPath,
        resultStamp);

    public bool IsCurrent() =>
        Index == ConflictResolutionFileStamp.Capture(IndexPath) &&
        Result == ConflictResolutionFileStamp.Capture(ResultPath);
}

internal readonly record struct ConflictResolutionFileStamp(
    bool Exists,
    long Length,
    long LastWriteUtcTicks)
{
    public static ConflictResolutionFileStamp Capture(string path)
    {
        var info = new FileInfo(path);
        return info.Exists
            ? new ConflictResolutionFileStamp(true, info.Length, info.LastWriteTimeUtc.Ticks)
            : default;
    }
}
