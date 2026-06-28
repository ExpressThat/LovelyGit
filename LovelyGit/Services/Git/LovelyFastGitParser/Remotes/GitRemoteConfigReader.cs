namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

internal static class GitRemoteConfigReader
{
    public static async Task<string?> ReadPrimaryRemoteUrlAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(gitDirectory, "config");
        if (!File.Exists(configPath))
        {
            return null;
        }

        string? remoteName = null;
        string? firstRemoteUrl = null;
        foreach (var rawLine in await File.ReadAllLinesAsync(configPath, cancellationToken).ConfigureAwait(false))
        {
            var line = rawLine.AsSpan().Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
            {
                continue;
            }

            if (TryReadRemoteSection(line, out var name))
            {
                remoteName = name;
                continue;
            }

            if (remoteName == null || !TryReadConfigValue(line, "url", out var url))
            {
                continue;
            }

            firstRemoteUrl ??= url;
            if (remoteName.Equals("origin", StringComparison.Ordinal))
            {
                return url;
            }
        }

        return firstRemoteUrl;
    }

    private static bool TryReadRemoteSection(ReadOnlySpan<char> line, out string name)
    {
        name = string.Empty;
        const string prefix = "[remote \"";
        if (!line.StartsWith(prefix, StringComparison.Ordinal) || !line.EndsWith("\"]", StringComparison.Ordinal))
        {
            return false;
        }

        name = line[prefix.Length..^2].ToString();
        return name.Length > 0;
    }

    private static bool TryReadConfigValue(ReadOnlySpan<char> line, string expectedKey, out string value)
    {
        value = string.Empty;
        var separator = line.IndexOf('=');
        if (separator <= 0)
        {
            return false;
        }

        var key = line[..separator].Trim();
        if (!key.Equals(expectedKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        value = line[(separator + 1)..].Trim().Trim('"').ToString();
        return value.Length > 0;
    }
}
