namespace ExpressThat.LovelyGit.Services.Git.Branches;

internal static class GitBranchUpstreamConfigReader
{
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
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
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
        const string HeadsPrefix = "refs/heads/";
        if (branchName == null || remoteName == null ||
            mergeRef == null || !mergeRef.StartsWith(HeadsPrefix, StringComparison.Ordinal))
        {
            return;
        }

        var trackedBranch = mergeRef[HeadsPrefix.Length..];
        var upstreamName = remoteName == "." ? trackedBranch : $"{remoteName}/{trackedBranch}";
        results.Add(new GitBranchUpstream(branchName, upstreamName));
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

internal sealed record GitBranchUpstream(string BranchName, string UpstreamName);
