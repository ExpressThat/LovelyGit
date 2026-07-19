namespace LovelyGit.Tests.Git;

internal static class RemoteRepositoryTemplate
{
    public static void RetargetConfig(string repositoryPath, string oldRoot, string newRoot)
    {
        var configPath = Path.Combine(repositoryPath, ".git", "config");
        var config = File.ReadAllText(configPath);
        File.WriteAllText(
            configPath,
            config.Replace(Escape(oldRoot), Escape(newRoot), StringComparison.Ordinal));
    }

    private static string Escape(string path) =>
        path.Replace("\\", "\\\\", StringComparison.Ordinal);
}
