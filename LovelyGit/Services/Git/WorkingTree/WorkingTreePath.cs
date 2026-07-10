namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class WorkingTreePath
{
    public static string NormalizeRelative(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
        {
            throw new InvalidOperationException("A repository-relative path is required.");
        }

        var remaining = path.AsSpan();
        while (!remaining.IsEmpty)
        {
            var separator = remaining.IndexOfAny('/', '\\');
            var segment = separator < 0 ? remaining : remaining[..separator];
            if (segment.IsEmpty || segment.SequenceEqual("..") || segment.SequenceEqual("."))
            {
                throw new InvalidOperationException("The conflict path is not safe.");
            }

            if (separator < 0)
            {
                break;
            }

            remaining = remaining[(separator + 1)..];
        }

        return path.Replace('\\', '/');
    }

    public static string Resolve(string workTreeDirectory, string normalizedPath)
    {
        var root = Path.GetFullPath(workTreeDirectory) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root, normalizedPath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(root, OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The conflict path escapes the working tree.");
        }

        return fullPath;
    }
}
