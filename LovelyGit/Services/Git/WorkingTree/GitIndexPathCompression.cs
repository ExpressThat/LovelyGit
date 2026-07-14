namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class GitIndexPathCompression
{
    public static string Restore(string previousPath, int removeLength, string suffix)
    {
        if ((uint)removeLength > (uint)previousPath.Length)
        {
            throw new InvalidDataException("Git index v4 path prefix is invalid.");
        }

        return string.Concat(previousPath.AsSpan(0, previousPath.Length - removeLength), suffix);
    }
}
