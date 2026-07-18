using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using System.Text;

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
        if (!enabled)
        {
            return new SparseCheckoutState();
        }

        var specification = await ReadPatternTextAsync(
                specificationPath,
                settings.ConeMode,
                cancellationToken)
            .ConfigureAwait(false);
        return new SparseCheckoutState
        {
            Enabled = true,
            ConeMode = settings.ConeMode,
            PatternCount = specification.Count,
            PatternText = specification.Text,
        };
    }

    internal static List<string> ReadPatterns(string[] lines, bool coneMode)
    {
        var collector = new PatternCollector(coneMode, lines.Length);
        foreach (var line in lines) collector.Add(line);
        return collector.Complete();
    }

    private static async Task<PatternText> ReadPatternTextAsync(
        string path,
        bool coneMode,
        CancellationToken cancellationToken)
    {
        var byteLength = new FileInfo(path).Length;
        var estimatedCount = (int)Math.Min(byteLength / 32 + 1, 250_000);
        var collector = new PatternTextCollector(coneMode, estimatedCount, byteLength);
        await PooledTextLineReader.ReadAsync(
                path,
                collector,
                static (line, state) => state.Add(line),
                cancellationToken)
            .ConfigureAwait(false);
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

    private sealed class PatternTextCollector(bool coneMode, int countCapacity, long byteLength)
    {
        private readonly PatternCollector? _coneCollector = coneMode
            ? new PatternCollector(true, countCapacity)
            : null;
        private readonly StringBuilder? _text = coneMode
            ? null
            : new StringBuilder((int)Math.Min(byteLength, 16 * 1024 * 1024));
        private int _count;

        public void Add(ReadOnlySpan<char> source)
        {
            if (_coneCollector != null)
            {
                _coneCollector.Add(source.ToString());
                return;
            }

            var line = source.Trim();
            if (line.IsEmpty || line[0] == '#') return;
            if (_count > 0) _text!.Append('\n');
            _text!.Append(line);
            _count++;
        }

        public PatternText Complete()
        {
            if (_coneCollector == null) return new PatternText(_text!.ToString(), _count);
            var patterns = _coneCollector.Complete();
            return new PatternText(string.Join('\n', patterns), patterns.Count);
        }
    }

    private sealed record PatternText(string Text, int Count);
}
