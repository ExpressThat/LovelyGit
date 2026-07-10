namespace ExpressThat.LovelyGit.Services.Git.Configuration;

internal sealed record GitIdentityReadOptions(
    string? HomeDirectory,
    string? XdgConfigHome,
    IReadOnlyList<string> SystemConfigPaths,
    IReadOnlyList<string> GlobalConfigPaths,
    IReadOnlyDictionary<string, string?> Environment)
{
    public static GitIdentityReadOptions CreateCurrent(string? gitRootDirectory = null)
    {
        var environment = ReadRelevantEnvironment();
        var home = Get(environment, "HOME") ?? System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.UserProfile);
        var xdg = Get(environment, "XDG_CONFIG_HOME");
        return new GitIdentityReadOptions(
            home,
            xdg,
            ResolveSystemPaths(environment, gitRootDirectory),
            ResolveGlobalPaths(environment, home, xdg),
            environment);
    }

    public string? GetEnvironment(string name) => Get(Environment, name);

    private static IReadOnlyDictionary<string, string?> ReadRelevantEnvironment()
    {
        var values = new Dictionary<string, string?>(16, StringComparer.OrdinalIgnoreCase);
        AddEnvironment(values, "HOME");
        AddEnvironment(values, "XDG_CONFIG_HOME");
        AddEnvironment(values, "PROGRAMDATA");
        AddEnvironment(values, "PROGRAMFILES");
        AddEnvironment(values, "GIT_CONFIG_NOSYSTEM");
        AddEnvironment(values, "GIT_CONFIG_SYSTEM");
        AddEnvironment(values, "GIT_CONFIG_GLOBAL");
        AddEnvironment(values, "GIT_COMMITTER_NAME");
        AddEnvironment(values, "GIT_COMMITTER_EMAIL");
        AddEnvironment(values, "GIT_AUTHOR_NAME");
        AddEnvironment(values, "GIT_AUTHOR_EMAIL");
        AddEnvironment(values, "GIT_CONFIG_COUNT");
        if (!int.TryParse(Get(values, "GIT_CONFIG_COUNT"), out var count) || count <= 0)
        {
            return values;
        }

        for (var index = 0; index < count; index++)
        {
            AddEnvironment(values, $"GIT_CONFIG_KEY_{index}");
            AddEnvironment(values, $"GIT_CONFIG_VALUE_{index}");
        }

        return values;
    }

    private static void AddEnvironment(Dictionary<string, string?> values, string name)
    {
        var value = System.Environment.GetEnvironmentVariable(name);
        if (value is not null)
        {
            values.Add(name, value);
        }
    }

    internal static IReadOnlyList<string> ResolveSystemPaths(
        IReadOnlyDictionary<string, string?> environment,
        string? gitRootDirectory)
    {
        if (IsTrue(Get(environment, "GIT_CONFIG_NOSYSTEM")))
        {
            return [];
        }

        if (Get(environment, "GIT_CONFIG_SYSTEM") is { Length: > 0 } configured)
        {
            return [configured];
        }

        if (!string.IsNullOrWhiteSpace(gitRootDirectory))
        {
            return [Path.Combine(gitRootDirectory, "etc", "gitconfig")];
        }

        var paths = new List<string>(2);
        AddIfPresent(paths, Get(environment, "PROGRAMDATA"), "Git", "config");
        AddIfPresent(paths, Get(environment, "PROGRAMFILES"), "Git", "etc", "gitconfig");
        if (!OperatingSystem.IsWindows())
        {
            paths.Add("/etc/gitconfig");
        }

        return paths;
    }

    private static IReadOnlyList<string> ResolveGlobalPaths(
        IReadOnlyDictionary<string, string?> environment,
        string? home,
        string? xdg)
    {
        if (Get(environment, "GIT_CONFIG_GLOBAL") is { Length: > 0 } configured)
        {
            return [configured];
        }

        var paths = new List<string>(2);
        var configHome = string.IsNullOrWhiteSpace(xdg)
            ? Combine(home, ".config")
            : xdg;
        AddIfPresent(paths, configHome, "git", "config");
        AddIfPresent(paths, home, ".gitconfig");
        return paths;
    }

    private static void AddIfPresent(List<string> paths, string? root, params string[] parts)
    {
        if (!string.IsNullOrWhiteSpace(root))
        {
            paths.Add(Path.Combine([root, .. parts]));
        }
    }

    private static string? Combine(string? root, string child) =>
        string.IsNullOrWhiteSpace(root) ? null : Path.Combine(root, child);

    private static string? Get(IReadOnlyDictionary<string, string?> values, string name) =>
        values.TryGetValue(name, out var value) ? value : null;

    private static bool IsTrue(string? value) =>
        value is not null && !value.Equals("0", StringComparison.Ordinal);
}
