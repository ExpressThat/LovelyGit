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
        return new SparseCheckoutState
        {
            Enabled = enabled,
            ConeMode = enabled && settings.ConeMode,
            Patterns = enabled
                ? await ReadPatternsAsync(specificationPath, settings.ConeMode, cancellationToken)
                    .ConfigureAwait(false)
                : [],
        };
    }

    internal static List<string> ReadPatterns(string[] lines, bool coneMode)
    {
        var collector = new PatternCollector(coneMode, lines.Length);
        foreach (var line in lines) collector.Add(line);
        return collector.Complete();
    }

    private static async Task<List<string>> ReadPatternsAsync(
        string path,
        bool coneMode,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            16 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var capacity = (int)Math.Min(stream.Length / 32 + 1, 250_000);
        var collector = new PatternCollector(coneMode, capacity);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            collector.Add(line);
        }
        return collector.Complete();
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

    private sealed class PatternCollector(bool coneMode, int capacity)
    {
        private readonly HashSet<string>? _excludedParents = coneMode
            ? new HashSet<string>(StringComparer.Ordinal)
            : null;
        private readonly List<string> _patterns = new(capacity);

        public void Add(string source)
        {
            var line = source.AsSpan().Trim();
            if (!coneMode)
            {
                if (!line.IsEmpty && line[0] != '#')
                {
                    _patterns.Add(line.Length == source.Length ? source : line.ToString());
                }
                return;
            }

            if (line.Length > 4 && line.StartsWith("!/") && line.EndsWith("/*/"))
            {
                _excludedParents!.Add(line[2..^3].ToString());
            }
            else if (line.Length >= 3 && line[0] == '/' && line[1] != '*' && line[^1] == '/')
            {
                _patterns.Add(line[1..^1].ToString());
            }
        }

        public List<string> Complete()
        {
            if (_excludedParents != null)
            {
                _patterns.RemoveAll(_excludedParents.Contains);
            }
            return _patterns;
        }
    }
}
