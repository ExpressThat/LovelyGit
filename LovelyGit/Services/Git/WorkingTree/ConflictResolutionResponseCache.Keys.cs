namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionResponseCache
{
    private static string Key(
        string repositoryPath,
        string path,
        string fingerprint,
        bool ignoreWhitespace) =>
        $"{Prefix(repositoryPath, path)}{fingerprint}\0{ignoreWhitespace}";

    private static string Prefix(string repositoryPath, string path) =>
        $"{NormalizeRepositoryPath(repositoryPath)}\0{path}\0";

    private static string NormalizeRepositoryPath(string path)
    {
        var normalized = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return OperatingSystem.IsWindows() ? normalized.ToUpperInvariant() : normalized;
    }
}
