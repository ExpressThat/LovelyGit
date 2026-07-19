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
        var configPath = Path.Combine(gitDirectory, "config");
        if (!File.Exists(configPath))
        {
            return [];
        }

        var state = new RemoteListParseState();
        await PooledTextLineReader.ReadAsync(
                configPath,
                state,
                static (line, parseState) => parseState.ProcessLine(line),
                cancellationToken)
            .ConfigureAwait(false);
        return state.CreateRemotes();
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

    private sealed class RemoteListParseState
    {
        private readonly Dictionary<string, GitRemote> _remotes = new(StringComparer.Ordinal);
        private GitRemote? _current;

        public void ProcessLine(ReadOnlySpan<char> rawLine)
        {
            var line = rawLine.Trim();
            if (line.IsEmpty || line[0] is '#' or ';')
            {
                return;
            }
            if (TryReadRemoteSectionName(line, out var name))
            {
                var key = name.ToString();
                if (!_remotes.TryGetValue(key, out _current))
                {
                    _current = new GitRemote { Name = key };
                    _remotes.Add(key, _current);
                }
                return;
            }
            if (line[0] == '[' && line[^1] == ']')
            {
                _current = null;
                return;
            }
            if (_current == null)
            {
                return;
            }
            if (TryReadConfigValue(line, "url", out var url))
            {
                _current.Url = url;
            }
            else if (TryReadConfigValue(line, "pushurl", out var pushUrl))
            {
                _current.PushUrl = pushUrl;
            }
        }

        public List<GitRemote> CreateRemotes() =>
            _remotes.Values.Where(remote => remote.Url.Length > 0).ToList();
    }

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
