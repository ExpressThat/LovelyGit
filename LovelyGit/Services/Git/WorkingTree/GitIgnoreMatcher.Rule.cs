using System.Text;
using System.Text.RegularExpressions;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class GitIgnoreMatcher
{
    private sealed class GitIgnoreRule
    {
        private readonly Regex? _regex;
        private readonly string? _literal;
        private readonly string _basePrefix;
        private readonly bool _anchored;
        private readonly bool _directoryOnly;

        private GitIgnoreRule(
            Regex? regex,
            string? literal,
            string basePrefix,
            bool anchored,
            bool isNegation,
            bool directoryOnly)
        {
            _regex = regex;
            _literal = literal;
            _basePrefix = basePrefix;
            _anchored = anchored;
            IsNegation = isNegation;
            _directoryOnly = directoryOnly;
        }

        public bool IsNegation { get; }

        public static GitIgnoreRule Create(string pattern, string baseDirectory, bool isNegation)
        {
            var directoryOnly = pattern.EndsWith("/", StringComparison.Ordinal);
            pattern = pattern.TrimEnd('/');
            var anchored = pattern.StartsWith("/", StringComparison.Ordinal) ||
                pattern.Contains('/', StringComparison.Ordinal);
            pattern = pattern.TrimStart('/');
            var normalizedBase = baseDirectory.Trim('/');
            var basePrefix = normalizedBase.Length == 0 ? string.Empty : normalizedBase + "/";

            if (pattern.IndexOf('*') < 0 && pattern.IndexOf('?') < 0 &&
                pattern.IndexOf('[') < 0 && pattern.IndexOf('\\') < 0)
            {
                return new GitIgnoreRule(
                    null, pattern, basePrefix, anchored, isNegation, directoryOnly);
            }

            var prefix = string.IsNullOrEmpty(baseDirectory)
                ? string.Empty
                : Regex.Escape(baseDirectory.Trim('/') + "/");
            var regexPattern = anchored || prefix.Length > 0
                ? "^" + prefix + (anchored ? string.Empty : "(?:.*/)?") + ConvertGlob(pattern)
                : "^(?:.*/)?" + ConvertGlob(pattern);

            regexPattern += "(?:/.*)?$";
            return new GitIgnoreRule(
                new Regex(regexPattern, RegexOptions.CultureInvariant | RegexOptions.Compiled),
                null,
                basePrefix,
                anchored,
                isNegation,
                directoryOnly);
        }

        public bool IsMatch(string relativePath, bool isDirectory)
        {
            if (_directoryOnly && !isDirectory) return false;
            return _literal == null
                ? _regex!.IsMatch(relativePath)
                : IsLiteralMatch(relativePath.AsSpan());
        }

        private bool IsLiteralMatch(ReadOnlySpan<char> path)
        {
            if (_basePrefix.Length > 0)
            {
                if (!path.StartsWith(_basePrefix, StringComparison.Ordinal)) return false;
                path = path[_basePrefix.Length..];
            }
            if (_anchored) return MatchesAt(path, 0);
            var start = 0;
            while (true)
            {
                if (MatchesAt(path, start)) return true;
                var separator = path[start..].IndexOf('/');
                if (separator < 0) return false;
                start += separator + 1;
            }
        }

        private bool MatchesAt(ReadOnlySpan<char> path, int start)
        {
            var literal = _literal.AsSpan();
            if (start + literal.Length > path.Length ||
                !path.Slice(start, literal.Length).SequenceEqual(literal)) return false;
            var end = start + literal.Length;
            return end == path.Length || path[end] == '/';
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
