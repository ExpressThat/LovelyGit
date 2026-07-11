namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal static class GitAlternateObjectDirectoryReader
{
    public static IReadOnlyList<string> Read(string gitDirectory)
    {
        var primary = Path.GetFullPath(Path.Combine(gitDirectory, "objects"));
        var directories = new List<string>();
        var pending = new Queue<string>();
        var seen = new HashSet<string>(GetPathComparer());
        pending.Enqueue(primary);

        while (pending.TryDequeue(out var objectDirectory))
        {
            if (!seen.Add(objectDirectory) || !Directory.Exists(objectDirectory))
            {
                continue;
            }

            directories.Add(objectDirectory);
            EnqueueAlternates(objectDirectory, pending);
        }

        return directories;
    }

    private static void EnqueueAlternates(string objectDirectory, Queue<string> pending)
    {
        var alternatesPath = Path.Combine(objectDirectory, "info", "alternates");
        if (!File.Exists(alternatesPath))
        {
            return;
        }

        try
        {
            foreach (var rawLine in File.ReadLines(alternatesPath))
            {
                var path = rawLine.Trim();
                if (path.Length == 0)
                {
                    continue;
                }

                try
                {
                    pending.Enqueue(Path.GetFullPath(path, objectDirectory));
                }
                catch (Exception exception) when (exception is ArgumentException
                                                   or NotSupportedException
                                                   or PathTooLongException)
                {
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static StringComparer GetPathComparer() =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
}
