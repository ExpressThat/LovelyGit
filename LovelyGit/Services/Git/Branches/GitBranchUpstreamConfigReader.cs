using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Branches;

internal static class GitBranchUpstreamConfigReader
{
    public static async Task<GitBranchUpstream?> ReadForBranchAsync(
        string gitDirectory,
        string targetBranchName,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(gitDirectory, "config");
        if (!File.Exists(path)) return null;
        var state = new TargetParseState(targetBranchName);
        await GitConfigLineReader.ReadAsync(
                path,
                state,
                static (line, parseState) => parseState.ProcessLine(line),
                cancellationToken)
            .ConfigureAwait(false);
        state.Complete();
        return state.Upstream;
    }

    public static async Task<List<GitBranchUpstream>> ReadAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(gitDirectory, "config");
        if (!File.Exists(path))
        {
            return [];
        }

        var results = new List<GitBranchUpstream>();
        string? branchName = null;
        string? remoteName = null;
        string? mergeRef = null;
        await using var stream = OpenConfig(path);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } rawLine)
        {
            var line = rawLine.AsSpan().Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
            {
                continue;
            }

            if (line[0] == '[' && line[^1] == ']')
            {
                Add(results, branchName, remoteName, mergeRef);
                branchName = TryReadBranchName(line, out var name) ? name : null;
                remoteName = null;
                mergeRef = null;
                continue;
            }

            if (branchName == null || !TryReadValue(line, out var key, out var value))
            {
                continue;
            }

            if (key.Equals("remote", StringComparison.OrdinalIgnoreCase))
            {
                remoteName = value;
            }
            else if (key.Equals("merge", StringComparison.OrdinalIgnoreCase))
            {
                mergeRef = value;
            }
        }

        Add(results, branchName, remoteName, mergeRef);
        results.Sort((left, right) => string.Compare(left.BranchName, right.BranchName, StringComparison.Ordinal));
        return results;
    }

    private static void Add(
        List<GitBranchUpstream> results,
        string? branchName,
        string? remoteName,
        string? mergeRef)
    {
        if (branchName != null && Create(branchName, remoteName, mergeRef) is { } upstream)
            results.Add(upstream);
    }

    private static GitBranchUpstream? Create(
        string branchName,
        string? remoteName,
        string? mergeRef)
    {
        const string HeadsPrefix = "refs/heads/";
        if (remoteName == null || mergeRef == null ||
            !mergeRef.StartsWith(HeadsPrefix, StringComparison.Ordinal)) return null;
        var trackedBranch = mergeRef[HeadsPrefix.Length..];
        var upstreamName = remoteName == "." ? trackedBranch : $"{remoteName}/{trackedBranch}";
        var refName = remoteName == "." ? mergeRef : $"refs/remotes/{remoteName}/{trackedBranch}";
        return new GitBranchUpstream(branchName, upstreamName, refName);
    }

    private static bool IsBranchSection(ReadOnlySpan<char> line, string branchName)
    {
        const string Prefix = "[branch \"";
        return line.StartsWith(Prefix, StringComparison.Ordinal)
            && line.EndsWith("\"]", StringComparison.Ordinal)
            && line[Prefix.Length..^2].SequenceEqual(branchName);
    }

    private static FileStream OpenConfig(string path) => new(
        path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
        bufferSize: 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);

    private sealed class TargetParseState(string branchName)
    {
        private bool _inTargetSection;
        private string? _remoteName;
        private string? _mergeRef;
        public GitBranchUpstream? Upstream { get; private set; }

        public void ProcessLine(ReadOnlySpan<char> rawLine)
        {
            if (Upstream != null) return;
            var line = rawLine.Trim();
            if (line.IsEmpty || line[0] is '#' or ';') return;
            if (line[0] == '[' && line[^1] == ']')
            {
                if (_inTargetSection)
                {
                    Complete();
                    return;
                }
                _inTargetSection = IsBranchSection(line, branchName);
                _remoteName = null;
                _mergeRef = null;
                return;
            }
            if (!_inTargetSection || !TryReadValue(line, out var key, out var value)) return;
            if (key.Equals("remote", StringComparison.OrdinalIgnoreCase)) _remoteName = value;
            else if (key.Equals("merge", StringComparison.OrdinalIgnoreCase)) _mergeRef = value;
        }

        public void Complete()
        {
            if (_inTargetSection) Upstream = Create(branchName, _remoteName, _mergeRef);
            _inTargetSection = false;
        }
    }

    private static bool TryReadBranchName(ReadOnlySpan<char> line, out string name)
    {
        const string Prefix = "[branch \"";
        name = string.Empty;
        if (!line.StartsWith(Prefix, StringComparison.Ordinal) || !line.EndsWith("\"]", StringComparison.Ordinal))
        {
            return false;
        }

        name = line[Prefix.Length..^2].ToString();
        return name.Length > 0;
    }

    private static bool TryReadValue(ReadOnlySpan<char> line, out ReadOnlySpan<char> key, out string value)
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

internal sealed record GitBranchUpstream(
    string BranchName,
    string UpstreamName,
    string RefName);
