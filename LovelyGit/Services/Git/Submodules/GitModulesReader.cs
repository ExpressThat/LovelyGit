namespace ExpressThat.LovelyGit.Services.Git.Submodules;

internal static class GitModulesReader
{
    private const int MaximumSubmodules = 1_000;

    public static async Task<List<GitSubmoduleDefinition>> ReadAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(repositoryPath, ".gitmodules");
        if (!File.Exists(path)) return [];

        var results = new List<GitSubmoduleDefinition>();
        string? name = null;
        string? modulePath = null;
        string? url = null;
        string? branch = null;
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } raw)
        {
            var line = raw.AsSpan().Trim();
            if (line.Length == 0 || line[0] is '#' or ';') continue;
            if (line[0] == '[' && line[^1] == ']')
            {
                Add(results, name, modulePath, url, branch);
                if (results.Count >= MaximumSubmodules) break;
                name = ParseName(line);
                modulePath = null;
                url = null;
                branch = null;
                continue;
            }

            if (name == null || !TryReadValue(line, out var key, out var value)) continue;
            if (key.Equals("path", StringComparison.OrdinalIgnoreCase)) modulePath = value;
            else if (key.Equals("url", StringComparison.OrdinalIgnoreCase)) url = value;
            else if (key.Equals("branch", StringComparison.OrdinalIgnoreCase)) branch = value;
        }

        Add(results, name, modulePath, url, branch);
        return results;
    }

    private static void Add(
        List<GitSubmoduleDefinition> results,
        string? name,
        string? path,
        string? url,
        string? branch)
    {
        if (name != null && path != null && url != null && results.Count < MaximumSubmodules)
        {
            results.Add(new GitSubmoduleDefinition(name, path.Replace('\\', '/'), url, branch));
        }
    }

    private static string? ParseName(ReadOnlySpan<char> line)
    {
        const string Prefix = "[submodule \"";
        return line.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) && line.EndsWith("\"]")
            ? line[Prefix.Length..^2].ToString()
            : null;
    }

    private static bool TryReadValue(
        ReadOnlySpan<char> line,
        out ReadOnlySpan<char> key,
        out string value)
    {
        var separator = line.IndexOf('=');
        if (separator <= 0)
        {
            key = default;
            value = string.Empty;
            return false;
        }

        key = line[..separator].Trim();
        value = line[(separator + 1)..].Trim().Trim('"').ToString();
        return value.Length > 0;
    }
}
