namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class GitObjectStore
{
    public async Task<GitObjectId?> ResolveUniquePrefixAsync(
        string prefix,
        CancellationToken cancellationToken)
    {
        var normalized = prefix.Trim().ToLowerInvariant();
        if (normalized.Length < 4
            || normalized.Length > GitObjectId.GetTextLength(_objectFormat)
            || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            return null;
        }

        if (normalized.Length == GitObjectId.GetTextLength(_objectFormat))
            return GitObjectId.Parse(normalized, _objectFormat);

        var matches = new HashSet<GitObjectId>();
        AddLooseMatches(normalized, matches, maximumResults: 2);
        foreach (var index in await GetPackIndexesAsync(cancellationToken).ConfigureAwait(false))
        {
            index.AddIdsWithPrefix(normalized, _objectFormat, matches, 2, cancellationToken);
            if (matches.Count > 1) return null;
        }
        return matches.Count == 1 ? matches.Single() : null;
    }

    private void AddLooseMatches(string prefix, ISet<GitObjectId> matches, int maximumResults)
    {
        var directoryPrefix = prefix[..2];
        var filePrefix = prefix[2..];
        foreach (var objectsPath in _objectDirectories)
        {
            var directory = Path.Combine(objectsPath, directoryPrefix);
            if (!Directory.Exists(directory)) continue;
            foreach (var path in Directory.EnumerateFiles(directory, filePrefix + "*"))
            {
                var value = directoryPrefix + Path.GetFileName(path);
                if (GitObjectId.TryParse(value, _objectFormat, out var id)) matches.Add(id);
                if (matches.Count >= maximumResults) return;
            }
        }
    }
}
