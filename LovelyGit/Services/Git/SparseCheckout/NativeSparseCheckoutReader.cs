using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.SparseCheckout;

internal sealed class NativeSparseCheckoutReader
{
    public async Task<SparseCheckoutState> ReadAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var settings = new SparseConfigSettings();
        await ReadConfigAsync(Path.Combine(paths.GitDirectory, "config"), settings, cancellationToken)
            .ConfigureAwait(false);
        await ReadConfigAsync(
                Path.Combine(paths.WorktreeGitDirectory, "config.worktree"),
                settings,
                cancellationToken)
            .ConfigureAwait(false);

        var specificationPath = Path.Combine(
            paths.WorktreeGitDirectory,
            "info",
            "sparse-checkout");
        var enabled = settings.Enabled && File.Exists(specificationPath);
        var lines = enabled
            ? await File.ReadAllLinesAsync(specificationPath, cancellationToken).ConfigureAwait(false)
            : [];
        return new SparseCheckoutState
        {
            Enabled = enabled,
            ConeMode = enabled && settings.ConeMode,
            Patterns = enabled ? ReadPatterns(lines, settings.ConeMode) : [],
        };
    }

    internal static List<string> ReadPatterns(string[] lines, bool coneMode)
    {
        if (!coneMode)
        {
            return lines
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith('#'))
                .ToList();
        }

        var excludedParents = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            var trimmed = line.AsSpan().Trim();
            if (trimmed.Length > 4 && trimmed.StartsWith("!/") && trimmed.EndsWith("/*/"))
            {
                excludedParents.Add(trimmed[2..^3].ToString());
            }
        }

        var result = new List<string>();
        foreach (var line in lines)
        {
            var trimmed = line.AsSpan().Trim();
            if (trimmed.Length < 3 || trimmed[0] != '/' || trimmed[1] == '*' || trimmed[^1] != '/')
            {
                continue;
            }

            var directory = trimmed[1..^1].ToString();
            if (!excludedParents.Contains(directory)) result.Add(directory);
        }
        return result;
    }

    private static async Task ReadConfigAsync(
        string path,
        SparseConfigSettings settings,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return;
        var inCore = false;
        foreach (var rawLine in await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false))
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
            if (separator <= 0) continue;
            var key = line[..separator].Trim();
            var enabled = line[(separator + 1)..].Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
            if (key.Equals("sparseCheckout", StringComparison.OrdinalIgnoreCase)) settings.Enabled = enabled;
            if (key.Equals("sparseCheckoutCone", StringComparison.OrdinalIgnoreCase)) settings.ConeMode = enabled;
        }
    }

    private sealed class SparseConfigSettings
    {
        public bool Enabled { get; set; }
        public bool ConeMode { get; set; }
    }
}
