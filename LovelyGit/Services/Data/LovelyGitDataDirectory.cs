namespace ExpressThat.LovelyGit.Services.Data;

internal static class LovelyGitDataDirectory
{
    internal const string OverrideEnvironmentVariable = "LOVELYGIT_DATA_DIRECTORY";

    public static string GetFilePath(string fileName)
    {
        return GetFilePath(
            fileName,
            Environment.GetEnvironmentVariable(OverrideEnvironmentVariable));
    }

    internal static string GetFilePath(string fileName, string? overridePath)
    {
        var directory = Resolve(overridePath);
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, fileName);
    }

    internal static string Resolve(string? overridePath)
    {
        return string.IsNullOrWhiteSpace(overridePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LovelyGit")
            : Path.GetFullPath(overridePath);
    }
}
