using System.Text;
using System.Text.RegularExpressions;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class GitIgnoreMatcher
{
    private readonly List<GitIgnoreRule> _rules;

    private GitIgnoreMatcher(List<GitIgnoreRule> rules)
    {
        _rules = rules;
    }

    public static async Task<GitIgnoreMatcher> LoadAsync(
        string workTreeDirectory,
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var rules = new List<GitIgnoreRule>();
        await LoadGlobalExcludeRulesAsync(workTreeDirectory, rules, cancellationToken).ConfigureAwait(false);

        var infoExcludePath = Path.Combine(gitDirectory, "info", "exclude");
        if (File.Exists(infoExcludePath))
        {
            await LoadRulesFromFileAsync(infoExcludePath, string.Empty, rules, cancellationToken).ConfigureAwait(false);
        }

        var rootGitIgnorePath = Path.Combine(workTreeDirectory, ".gitignore");
        if (File.Exists(rootGitIgnorePath))
        {
            await LoadRulesFromFileAsync(rootGitIgnorePath, string.Empty, rules, cancellationToken).ConfigureAwait(false);
        }

        return new GitIgnoreMatcher(rules);
    }

    public async Task LoadRulesForDirectoryAsync(
        string workTreeDirectory,
        string relativeDirectory,
        CancellationToken cancellationToken)
    {
        relativeDirectory = NormalizePath(relativeDirectory);
        if (string.IsNullOrEmpty(relativeDirectory))
        {
            return;
        }

        var gitIgnorePath = Path.Combine(workTreeDirectory, relativeDirectory.Replace('/', Path.DirectorySeparatorChar), ".gitignore");
        if (File.Exists(gitIgnorePath))
        {
            await LoadRulesFromFileAsync(gitIgnorePath, relativeDirectory, _rules, cancellationToken).ConfigureAwait(false);
        }
    }

    public bool IsIgnored(string relativePath, bool isDirectory)
    {
        relativePath = NormalizePath(relativePath).TrimStart('/');
        var ignored = false;
        var parentIgnored = false;
        foreach (var parent in EnumerateParents(relativePath))
        {
            if (IsIgnoredCore(parent, true, allowNegation: false))
            {
                parentIgnored = true;
                break;
            }
        }

        foreach (var rule in _rules)
        {
            if (parentIgnored && rule.IsNegation)
            {
                continue;
            }

            if (!rule.IsMatch(relativePath, isDirectory))
            {
                continue;
            }

            ignored = !rule.IsNegation;
        }

        return ignored;
    }

    private bool IsIgnoredCore(string relativePath, bool isDirectory, bool allowNegation)
    {
        var ignored = false;
        foreach (var rule in _rules)
        {
            if (!allowNegation && rule.IsNegation)
            {
                continue;
            }

            if (rule.IsMatch(relativePath, isDirectory))
            {
                ignored = !rule.IsNegation;
            }
        }

        return ignored;
    }

    private static IEnumerable<string> EnumerateParents(string relativePath)
    {
        var index = relativePath.IndexOf('/');
        while (index >= 0)
        {
            yield return relativePath[..index];
            index = relativePath.IndexOf('/', index + 1);
        }
    }

    private static async Task LoadGlobalExcludeRulesAsync(
        string workTreeDirectory,
        List<GitIgnoreRule> rules,
        CancellationToken cancellationToken)
    {
        var excludesFile = await TryReadConfigValueAsync(
                Path.Combine(workTreeDirectory, ".git", "config"),
                "core",
                "excludesfile",
                cancellationToken)
            .ConfigureAwait(false);
        excludesFile ??= await TryReadConfigValueAsync(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".gitconfig"),
                "core",
                "excludesfile",
                cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(excludesFile))
        {
            return;
        }

        excludesFile = ExpandPath(excludesFile);
        if (File.Exists(excludesFile))
        {
            await LoadRulesFromFileAsync(excludesFile, string.Empty, rules, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<string?> TryReadConfigValueAsync(
        string path,
        string sectionName,
        string keyName,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var section = string.Empty;
        foreach (var rawLine in await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false))
        {
            var line = rawLine.AsSpan().Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
            {
                continue;
            }

            if (line[0] == '[' && line[^1] == ']')
            {
                section = line[1..^1].Trim().Trim('"').ToString();
                continue;
            }

            var separator = line.IndexOf('=');
            if (!section.Equals(sectionName, StringComparison.OrdinalIgnoreCase) || separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            if (key.Equals(keyName, StringComparison.OrdinalIgnoreCase))
            {
                return line[(separator + 1)..].Trim().Trim('"').ToString();
            }
        }

        return null;
    }

    private static async Task LoadRulesFromFileAsync(
        string path,
        string baseDirectory,
        List<GitIgnoreRule> rules,
        CancellationToken cancellationToken)
    {
        foreach (var rawLine in await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false))
        {
            var parsed = ParsePattern(rawLine);
            if (parsed == null)
            {
                continue;
            }

            rules.Add(GitIgnoreRule.Create(parsed.Value.Pattern, baseDirectory, parsed.Value.IsNegation));
        }
    }

    private static (string Pattern, bool IsNegation)? ParsePattern(string line)
    {
        if (line.Length == 0)
        {
            return null;
        }

        if (line[0] == '#')
        {
            return null;
        }

        var isNegation = line[0] == '!';
        if (isNegation)
        {
            line = line[1..];
        }

        line = TrimUnescapedTrailingSpaces(line);
        if (line.Length == 0)
        {
            return null;
        }

        if (line[0] == '\\' && line.Length > 1 && line[1] is '#' or '!')
        {
            line = line[1..];
        }

        return (line, isNegation);
    }

    private static string TrimUnescapedTrailingSpaces(string line)
    {
        var end = line.Length;
        while (end > 0 && line[end - 1] == ' ')
        {
            var slashCount = 0;
            for (var i = end - 2; i >= 0 && line[i] == '\\'; i--)
            {
                slashCount++;
            }

            if (slashCount % 2 == 1)
            {
                break;
            }

            end--;
        }

        return line[..end].Replace("\\ ", " ", StringComparison.Ordinal);
    }

    private static string ExpandPath(string path)
    {
        if (path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..]);
        }

        return Environment.ExpandEnvironmentVariables(path);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    private sealed class GitIgnoreRule
    {
        private readonly Regex _regex;
        private readonly bool _directoryOnly;

        private GitIgnoreRule(Regex regex, bool isNegation, bool directoryOnly)
        {
            _regex = regex;
            IsNegation = isNegation;
            _directoryOnly = directoryOnly;
        }

        public bool IsNegation { get; }

        public static GitIgnoreRule Create(string pattern, string baseDirectory, bool isNegation)
        {
            pattern = NormalizePath(pattern);
            var directoryOnly = pattern.EndsWith("/", StringComparison.Ordinal);
            pattern = pattern.TrimEnd('/');
            var anchored = pattern.Contains('/', StringComparison.Ordinal);
            if (pattern.StartsWith("/", StringComparison.Ordinal))
            {
                anchored = true;
                pattern = pattern.TrimStart('/');
            }

            var prefix = string.IsNullOrEmpty(baseDirectory)
                ? string.Empty
                : Regex.Escape(baseDirectory.Trim('/') + "/");
            var regexPattern = anchored
                ? "^" + prefix + ConvertGlob(pattern)
                : "^(?:" + prefix + ")?(?:.*/)?" + ConvertGlob(pattern);

            regexPattern += directoryOnly ? "(?:/.*)?$" : "(?:/.*)?$";
            return new GitIgnoreRule(
                new Regex(regexPattern, RegexOptions.CultureInvariant | RegexOptions.Compiled),
                isNegation,
                directoryOnly);
        }

        public bool IsMatch(string relativePath, bool isDirectory)
        {
            return (!_directoryOnly || isDirectory) && _regex.IsMatch(relativePath);
        }

        private static string ConvertGlob(string pattern)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < pattern.Length; i++)
            {
                var c = pattern[i];
                if (c == '*')
                {
                    if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                    {
                        i++;
                        if (i + 1 < pattern.Length && pattern[i + 1] == '/')
                        {
                            i++;
                            builder.Append("(?:.*/)?");
                        }
                        else
                        {
                            builder.Append(".*");
                        }
                    }
                    else
                    {
                        builder.Append("[^/]*");
                    }
                }
                else if (c == '?')
                {
                    builder.Append("[^/]");
                }
                else if (c == '[')
                {
                    var end = pattern.IndexOf(']', i + 1);
                    if (end > i)
                    {
                        builder.Append(pattern.AsSpan(i, end - i + 1));
                        i = end;
                    }
                    else
                    {
                        builder.Append("\\[");
                    }
                }
                else if (c == '\\' && i + 1 < pattern.Length)
                {
                    i++;
                    builder.Append(Regex.Escape(pattern[i].ToString()));
                }
                else
                {
                    builder.Append(Regex.Escape(c.ToString()));
                }
            }

            return builder.ToString();
        }
    }
}
