namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

internal static class GitRemoteConfigReader
{
    public static async Task<List<GitRemote>> ReadRemotesAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var remotes = new List<GitRemote>();
        await ReadRemoteConfigAsync(
                gitDirectory,
                (name, url) => remotes.Add(new GitRemote { Name = name, Url = url }),
                cancellationToken)
            .ConfigureAwait(false);
        remotes.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));
        return remotes;
    }

    public static async Task<string?> ReadPrimaryRemoteUrlAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        string? firstRemoteUrl = null;
        string? originUrl = null;
        await ReadRemoteConfigAsync(
                gitDirectory,
                (name, url) =>
                {
                    firstRemoteUrl ??= url;
                    if (name.Equals("origin", StringComparison.Ordinal))
                    {
                        originUrl = url;
                    }
                },
                cancellationToken)
            .ConfigureAwait(false);
        return originUrl ?? firstRemoteUrl;
    }

    private static async Task ReadRemoteConfigAsync(
        string gitDirectory,
        Action<string, string> onRemote,
        CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(gitDirectory, "config");
        if (!File.Exists(configPath))
        {
            return;
        }

        string? remoteName = null;
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

            onRemote(remoteName, url);
        }
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
