using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.Lfs;

internal sealed class NativeGitLfsStateReader
{
    private const int MaximumPatterns = 10_000;
    private readonly GitCliService _gitCliService;

    public NativeGitLfsStateReader(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task<LfsRepositoryState> ReadAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var patterns = await ReadPatternsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        return new LfsRepositoryState
        {
            IsAvailable = FindLfsExecutable(_gitCliService.Installation) != null,
            IsInitialized = await HasLfsPrePushHookAsync(
                    paths.GitDirectory,
                    cancellationToken)
                .ConfigureAwait(false),
            HasTrackedPatterns = patterns.Count > 0,
            TrackedPatterns = patterns,
        };
    }

    internal static string? FindLfsExecutable(GitCliInstallation installation)
    {
        var executableName = installation.OperatingSystem == GitCliOperatingSystem.Windows
            ? "git-lfs.exe"
            : "git-lfs";
        foreach (var directory in installation.PathDirectories)
        {
            var candidate = Path.Combine(directory, executableName);
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    internal static async Task<List<string>> ReadPatternsAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var attributesPath = Path.Combine(repositoryPath, ".gitattributes");
        if (!File.Exists(attributesPath)) return [];

        var patterns = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        await using var stream = new FileStream(
            attributesPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream);
        while (patterns.Count < MaximumPatterns &&
               await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } raw)
        {
            var line = raw.AsSpan().Trim();
            if (line.IsEmpty || line[0] == '#') continue;
            var separator = FindPatternEnd(line);
            if (separator <= 0) continue;
            var attributes = line[separator..].TrimStart();
            if (!HasLfsFilter(attributes)) continue;
            var pattern = DecodePattern(line[..separator]);
            if (seen.Add(pattern)) patterns.Add(pattern);
        }

        return patterns;
    }

    private static async Task<bool> HasLfsPrePushHookAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var hookPath = Path.Combine(gitDirectory, "hooks", "pre-push");
        if (!File.Exists(hookPath)) return false;

        var hook = await File.ReadAllTextAsync(hookPath, cancellationToken)
            .ConfigureAwait(false);
        return hook.Contains("git lfs pre-push", StringComparison.OrdinalIgnoreCase);
    }

    private static int FindPatternEnd(ReadOnlySpan<char> line)
    {
        var quoted = false;
        var escaped = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (character == '\\') escaped = true;
            else if (character == '"') quoted = !quoted;
            else if (!quoted && char.IsWhiteSpace(character)) return index;
        }

        return -1;
    }

    private static bool HasLfsFilter(ReadOnlySpan<char> attributes)
    {
        while (!attributes.IsEmpty)
        {
            var end = attributes.IndexOfAny(' ', '\t');
            var attribute = end < 0 ? attributes : attributes[..end];
            if (attribute.Equals("filter=lfs", StringComparison.Ordinal)) return true;
            if (end < 0) break;
            attributes = attributes[(end + 1)..].TrimStart();
        }

        return false;
    }

    private static string DecodePattern(ReadOnlySpan<char> pattern)
    {
        if (pattern.Length >= 2 && pattern[0] == '"' && pattern[^1] == '"')
        {
            pattern = pattern[1..^1];
        }

        var escape = pattern.IndexOf('\\');
        if (escape < 0) return DecodeLfsWhitespace(pattern.ToString());

        var result = new StringBuilder(pattern.Length);
        result.Append(pattern[..escape]);
        for (var index = escape; index < pattern.Length; index++)
        {
            var character = pattern[index];
            if (character != '\\' || index + 1 >= pattern.Length)
            {
                result.Append(character);
                continue;
            }

            character = pattern[++index];
            result.Append(character switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                _ => character,
            });
        }

        return DecodeLfsWhitespace(result.ToString());
    }

    private static string DecodeLfsWhitespace(string pattern) =>
        pattern.Replace("[[:space:]]", " ", StringComparison.Ordinal);
}
