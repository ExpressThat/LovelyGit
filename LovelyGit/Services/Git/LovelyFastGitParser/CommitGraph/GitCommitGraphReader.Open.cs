namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.CommitGraph;

internal sealed partial class GitCommitGraphReader
{
    public static GitCommitGraphReader? TryOpen(
        string gitDirectory,
        GitObjectFormat objectFormat)
    {
        try
        {
            return TryOpenCore(gitDirectory, objectFormat);
        }
        catch (Exception exception) when (exception is IOException
                                           or UnauthorizedAccessException
                                           or InvalidDataException
                                           or OverflowException)
        {
            return null;
        }
    }

    private static GitCommitGraphReader? TryOpenCore(
        string gitDirectory,
        GitObjectFormat objectFormat)
    {
        if (HasHistoryOverrides(gitDirectory)) return null;
        var objectDirectory = Path.Combine(gitDirectory, "objects");
        var paths = FindGraphPaths(objectDirectory, objectFormat);
        if (paths.Count == 0) return null;

        var layers = new List<GitCommitGraphLayer>(paths.Count);
        try
        {
            var basePosition = 0;
            for (var index = 0; index < paths.Count; index++)
            {
                var layer = GitCommitGraphLayer.Open(paths[index], objectFormat);
                if (layer.BaseGraphCount != index)
                    throw new InvalidDataException("Commit-graph chain base count is invalid.");
                layer.BasePosition = basePosition;
                basePosition = checked(basePosition + layer.LocalCount);
                layers.Add(layer);
            }
            return new GitCommitGraphReader(layers, objectFormat, basePosition);
        }
        catch
        {
            foreach (var layer in layers) layer.Dispose();
            throw;
        }
    }

    private static List<string> FindGraphPaths(
        string objectDirectory,
        GitObjectFormat objectFormat)
    {
        var single = Path.Combine(objectDirectory, "info", "commit-graph");
        if (File.Exists(single)) return [single];

        var chainDirectory = Path.Combine(objectDirectory, "info", "commit-graphs");
        var chainPath = Path.Combine(chainDirectory, "commit-graph-chain");
        if (!File.Exists(chainPath)) return [];
        var hashLength = GitObjectId.GetTextLength(objectFormat);
        var paths = new List<string>();
        foreach (var rawLine in File.ReadLines(chainPath))
        {
            var hash = rawLine.AsSpan().Trim();
            if (hash.Length != hashLength || !IsHex(hash)) return [];
            var graph = Path.Combine(chainDirectory, $"graph-{hash.ToString()}.graph");
            if (!File.Exists(graph)) return [];
            paths.Add(graph);
        }
        return paths;
    }

    private static bool HasHistoryOverrides(string gitDirectory)
    {
        if (IsCommitGraphDisabled(Path.Combine(gitDirectory, "config"))) return true;
        if (File.Exists(Path.Combine(gitDirectory, "shallow")) ||
            File.Exists(Path.Combine(gitDirectory, "info", "grafts")))
        {
            return true;
        }

        var replaceDirectory = Path.Combine(gitDirectory, "refs", "replace");
        if (Directory.Exists(replaceDirectory) &&
            Directory.EnumerateFiles(replaceDirectory, "*", SearchOption.AllDirectories).Any())
        {
            return true;
        }

        var packedRefs = Path.Combine(gitDirectory, "packed-refs");
        return File.Exists(packedRefs) && File.ReadLines(packedRefs)
            .Any(static line => line.Contains(" refs/replace/", StringComparison.Ordinal));
    }

    private static bool IsCommitGraphDisabled(string configPath)
    {
        if (!File.Exists(configPath)) return false;
        var inCore = false;
        var disabled = false;
        foreach (var rawLine in File.ReadLines(configPath))
        {
            var line = rawLine.AsSpan().Trim();
            if (line.Length == 0 || line[0] is '#' or ';') continue;
            if (line[0] == '[' && line[^1] == ']')
            {
                inCore = line[1..^1].Trim().Equals("core", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!inCore) continue;
            var separator = line.IndexOf('=');
            if (separator <= 0 || !line[..separator].Trim().Equals(
                    "commitgraph", StringComparison.OrdinalIgnoreCase)) continue;
            var value = line[(separator + 1)..].Trim();
            disabled = value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                       value.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                       value.Equals("off", StringComparison.OrdinalIgnoreCase) ||
                       value.SequenceEqual("0");
        }
        return disabled;
    }

    private static bool IsHex(ReadOnlySpan<char> value)
    {
        foreach (var character in value)
        {
            if (character is not (>= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
                return false;
        }
        return true;
    }
}
