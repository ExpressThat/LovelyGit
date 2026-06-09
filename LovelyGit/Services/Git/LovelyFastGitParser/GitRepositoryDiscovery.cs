namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal static class GitRepositoryDiscovery
{
    public static async Task<GitRepositoryPaths> ResolveRepositoryPathsAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        var attributes = File.GetAttributes(fullPath);
        if ((attributes & FileAttributes.Directory) == 0)
        {
            throw new DirectoryNotFoundException($"Path is not a directory: {path}");
        }

        if (Path.GetFileName(fullPath).Equals(".git", StringComparison.OrdinalIgnoreCase))
        {
            return new GitRepositoryPaths(fullPath, Directory.GetParent(fullPath)?.FullName ?? fullPath);
        }

        var dotGitPath = Path.Combine(fullPath, ".git");
        if (Directory.Exists(dotGitPath))
        {
            return new GitRepositoryPaths(dotGitPath, fullPath);
        }

        if (File.Exists(dotGitPath))
        {
            var text = (await File.ReadAllTextAsync(dotGitPath, cancellationToken).ConfigureAwait(false)).Trim();
            const string prefix = "gitdir:";
            if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(".git file does not contain a gitdir pointer.");
            }

            var gitDir = text.AsSpan(prefix.Length).Trim().ToString();
            return new GitRepositoryPaths(
                Path.GetFullPath(Path.IsPathRooted(gitDir) ? gitDir : Path.Combine(fullPath, gitDir)),
                fullPath);
        }

        throw new DirectoryNotFoundException($"Could not find .git directory for: {path}");
    }

    public static async Task<string> ResolveGitDirectoryAsync(string path, CancellationToken cancellationToken)
    {
        return (await ResolveRepositoryPathsAsync(path, cancellationToken).ConfigureAwait(false)).GitDirectory;
    }

    public static async Task<GitObjectFormat> ReadObjectFormatAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(gitDirectory, "config");
        if (!File.Exists(configPath))
        {
            return GitObjectFormat.Sha1;
        }

        var section = string.Empty;
        foreach (var rawLine in await File.ReadAllLinesAsync(configPath, cancellationToken).ConfigureAwait(false))
        {
            var line = rawLine.AsSpan().Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
            {
                continue;
            }

            if (line[0] == '[' && line[^1] == ']')
            {
                section = line[1..^1].Trim().Trim('"').ToString().ToLowerInvariant();
                continue;
            }

            if (!section.Equals("extensions", StringComparison.Ordinal) ||
                !TryReadConfigKeyValue(line, out var key, out var value) ||
                !key.Equals("objectformat", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var objectFormat = value.AsSpan().Trim();
            if (objectFormat.Equals("sha1", StringComparison.OrdinalIgnoreCase))
            {
                return GitObjectFormat.Sha1;
            }

            if (objectFormat.Equals("sha256", StringComparison.OrdinalIgnoreCase))
            {
                return GitObjectFormat.Sha256;
            }

            throw new NotSupportedException($"Unsupported Git object format: {value}");
        }

        return GitObjectFormat.Sha1;
    }

    private static bool TryReadConfigKeyValue(ReadOnlySpan<char> line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        var separator = line.IndexOf('=');
        if (separator <= 0)
        {
            return false;
        }

        key = line[..separator].Trim().ToString();
        value = line[(separator + 1)..].Trim().Trim('"').ToString();
        return key.Length > 0;
    }
}

internal sealed record GitRepositoryPaths(string GitDirectory, string WorkTreeDirectory);
