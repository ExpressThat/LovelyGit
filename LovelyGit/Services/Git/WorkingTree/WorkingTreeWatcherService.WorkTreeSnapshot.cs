namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeWatcherService : IDisposable
{
    internal static ulong ComputeWorkTreeSnapshot(
        string workTreeDirectory,
        GitIgnoreMatcher? matcher,
        CancellationToken cancellationToken = default)
    {
        ulong xor = 0;
        ulong sum = 0;
        ulong count = 0;
        foreach (var path in EnumerateWorkTreeSnapshotFiles(workTreeDirectory, matcher, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var itemHash = ComputeWorkTreeSnapshotItem(workTreeDirectory, path);
            xor ^= itemHash;
            sum += itemHash;
            count++;
        }

        var hash = FnvOffsetBasis;
        AddUInt64ToHash(ref hash, xor);
        AddUInt64ToHash(ref hash, sum);
        AddUInt64ToHash(ref hash, count);
        return hash;
    }

    private static IEnumerable<string> EnumerateWorkTreeSnapshotFiles(
        string workTreeDirectory,
        GitIgnoreMatcher? matcher,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workTreeDirectory) || !Directory.Exists(workTreeDirectory))
        {
            yield break;
        }

        var pending = new Stack<string>();
        pending.Push(workTreeDirectory);
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            foreach (var childDirectory in EnumerateDirectories(directory))
            {
                if (ShouldSkipWorkTreeSnapshotPath(workTreeDirectory, childDirectory, matcher, isDirectory: true)
                    || IsReparseDirectory(childDirectory))
                {
                    continue;
                }

                pending.Push(childDirectory);
            }

            foreach (var file in EnumerateFiles(directory))
            {
                if (!ShouldSkipWorkTreeSnapshotPath(workTreeDirectory, file, matcher, isDirectory: false))
                {
                    yield return file;
                }
            }
        }
    }

    private static bool ShouldSkipWorkTreeSnapshotPath(
        string workTreeDirectory,
        string path,
        GitIgnoreMatcher? matcher,
        bool isDirectory)
    {
        var relativePath = Path.GetRelativePath(workTreeDirectory, path).Replace('\\', '/');
        return relativePath.Equals(".git", StringComparison.Ordinal)
            || relativePath.StartsWith(".git/", StringComparison.Ordinal)
            || matcher?.IsIgnored(relativePath, isDirectory) == true;
    }

    private static IEnumerable<string> EnumerateFiles(string directory)
    {
        try
        {
            return Directory.EnumerateFiles(directory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static bool IsReparseDirectory(string directory)
    {
        try
        {
            return (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static ulong ComputeWorkTreeSnapshotItem(string workTreeDirectory, string path)
    {
        var hash = FnvOffsetBasis;
        AddStringToHash(ref hash, Path.GetRelativePath(workTreeDirectory, path).Replace('\\', '/'));
        try
        {
            var info = new FileInfo(path);
            AddUInt64ToHash(ref hash, unchecked((ulong)info.Length));
            AddUInt64ToHash(ref hash, unchecked((ulong)info.LastWriteTimeUtc.Ticks));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            AddStringToHash(ref hash, path);
        }

        return hash;
    }
}
