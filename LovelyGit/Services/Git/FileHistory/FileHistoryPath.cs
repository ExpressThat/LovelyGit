namespace ExpressThat.LovelyGit.Services.Git.FileHistory;

internal static class FileHistoryPath
{
    private const int MaximumLength = 4096;

    public static string Normalize(string path)
    {
        var normalized = path.Trim().Replace('\\', '/').TrimStart('/');
        if (normalized.Length == 0 || normalized.Length > MaximumLength || normalized.Contains('\0'))
        {
            throw new ArgumentException("A valid repository-relative file path is required.", nameof(path));
        }

        foreach (var segment in normalized.Split('/'))
        {
            if (segment.Length == 0 || segment is "." or "..")
            {
                throw new ArgumentException("The file path must stay within the repository.", nameof(path));
            }
        }

        return normalized;
    }
}
