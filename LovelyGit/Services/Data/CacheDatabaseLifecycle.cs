namespace ExpressThat.LovelyGit.Services.Data;

internal static class CacheDatabaseLifecycle
{
    public static bool IsCurrent(
        string databasePath,
        string versionPath,
        string expectedVersion)
    {
        if (!File.Exists(databasePath) || !File.Exists(versionPath))
        {
            return false;
        }

        try
        {
            return string.Equals(
                File.ReadAllText(versionPath),
                expectedVersion,
                StringComparison.Ordinal);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static void WriteVersion(string versionPath, string version)
    {
        File.WriteAllText(versionPath, version);
    }
}
