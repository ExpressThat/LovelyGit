namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

internal static class GitRemoteConfigReader
{
    public static async Task<List<GitRemote>> ReadRemotesAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var remotes = await ReadRemoteConfigAsync(gitDirectory, cancellationToken).ConfigureAwait(false);
        remotes.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));
        return remotes;
    }

    public static async Task<GitRemote?> ReadRemoteAsync(
        string gitDirectory,
        string remoteName,
        CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(gitDirectory, "config");
        if (!File.Exists(configPath))
        {
            return null;
        }

        var state = new TargetRemoteParseState(remoteName);
        await PooledTextLineReader.ReadAsync(
                configPath,
                state,
                static (line, parseState) => parseState.ProcessLine(line),
                cancellationToken)
            .ConfigureAwait(false);
        return state.CreateRemote();
    }

    public static async Task<string?> ReadPrimaryRemoteUrlAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
        => await GitPrimaryRemoteUrlReader.ReadAsync(gitDirectory, cancellationToken)
            .ConfigureAwait(false);

    private static async Task<List<GitRemote>> ReadRemoteConfigAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var remotes = new Dictionary<string, GitRemote>(StringComparer.Ordinal);
        var configPath = Path.Combine(gitDirectory, "config");
        if (!File.Exists(configPath))
        {
            return [];
        }

        string? remoteName = null;
        await using var stream = OpenConfig(configPath);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } rawLine)
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

            if (line[0] == '[' && line[^1] == ']')
            {
                remoteName = null;
                continue;
            }

            if (remoteName == null)
            {
                continue;
            }

            if (!remotes.TryGetValue(remoteName, out var remote))
            {
                remote = new GitRemote { Name = remoteName };
                remotes.Add(remoteName, remote);
            }
            if (TryReadConfigValue(line, "url", out var url))
            {
                remote.Url = url;
            }
            else if (TryReadConfigValue(line, "pushurl", out var pushUrl))
            {
                remote.PushUrl = pushUrl;
            }
        }

        return remotes.Values.Where(remote => remote.Url.Length > 0).ToList();
    }

    private static bool TryReadRemoteSection(ReadOnlySpan<char> line, out string name)
    {
        if (TryReadRemoteSectionName(line, out var value))
        {
            name = value.ToString();
            return true;
        }
        name = string.Empty;
        return false;
    }

    internal static bool TryReadRemoteSectionName(
        ReadOnlySpan<char> line,
        out ReadOnlySpan<char> name)
    {
        const string prefix = "[remote \"";
        if (!line.StartsWith(prefix, StringComparison.Ordinal) || !line.EndsWith("\"]", StringComparison.Ordinal))
        {
            name = default;
            return false;
        }

        name = line[prefix.Length..^2];
        return !name.IsEmpty;
    }

    internal static bool TryReadConfigValue(
        ReadOnlySpan<char> line,
        string expectedKey,
        out string value)
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

    private static FileStream OpenConfig(string path) => new(
        path,
        FileMode.Open,
        FileAccess.Read,
        FileShare.ReadWrite | FileShare.Delete,
        bufferSize: 4096,
        FileOptions.Asynchronous | FileOptions.SequentialScan);

    private sealed class TargetRemoteParseState(string targetName)
    {
        private bool _inTargetSection;
        private string? _pushUrl;
        private string? _url;

        public void ProcessLine(ReadOnlySpan<char> rawLine)
        {
            var line = rawLine.Trim();
            if (line.IsEmpty || line[0] is '#' or ';')
            {
                return;
            }

            if (TryReadRemoteSectionName(line, out var name))
            {
                _inTargetSection = name.SequenceEqual(targetName);
                return;
            }

            if (line[0] == '[' && line[^1] == ']')
            {
                _inTargetSection = false;
                return;
            }

            if (!_inTargetSection)
            {
                return;
            }

            if (TryReadConfigValue(line, "url", out var url))
            {
                _url = url;
            }
            else if (TryReadConfigValue(line, "pushurl", out var pushUrl))
            {
                _pushUrl = pushUrl;
            }
        }

        public GitRemote? CreateRemote() =>
            string.IsNullOrEmpty(_url)
                ? null
                : new GitRemote { Name = targetName, PushUrl = _pushUrl, Url = _url };
    }
}
