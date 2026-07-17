using System.Text;
using System.Text.RegularExpressions;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class GitIgnoreMatcher
{
    private sealed class GitIgnoreRule
    {
        private readonly Regex? _regex;
        private readonly string? _literal;
        private readonly string? _suffix;
        private readonly string _basePrefix;
        private readonly bool _anchored;
        private readonly bool _directoryOnly;

        private GitIgnoreRule(
            Regex? regex,
            string? literal,
            string? suffix,
            string basePrefix,
            bool anchored,
            bool isNegation,
            bool directoryOnly)
        {
            _regex = regex;
            _literal = literal;
            _suffix = suffix;
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

            if (IsSimpleSuffixGlob(pattern))
            {
                return new GitIgnoreRule(
                    null, null, pattern[1..], basePrefix, anchored,
                    isNegation, directoryOnly);
            }

            if (pattern.IndexOf('*') < 0 && pattern.IndexOf('?') < 0 &&
                pattern.IndexOf('[') < 0 && pattern.IndexOf('\\') < 0)
            {
                return new GitIgnoreRule(
                    null, pattern, null, basePrefix, anchored, isNegation, directoryOnly);
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
                null,
                basePrefix,
                anchored,
                isNegation,
                directoryOnly);
        }

        public bool IsMatch(string relativePath, bool isDirectory)
        {
            if (_directoryOnly && !isDirectory) return false;
            if (_literal != null) return IsLiteralMatch(relativePath.AsSpan());
            if (_suffix != null) return IsSuffixMatch(relativePath.AsSpan());
            return _regex!.IsMatch(relativePath);
        }

        private static bool IsSimpleSuffixGlob(string pattern) =>
            pattern.Length > 1 && pattern[0] == '*' &&
            pattern.IndexOf('*', 1) < 0 && pattern.IndexOf('?') < 0 &&
            pattern.IndexOf('[') < 0 && pattern.IndexOf('\\') < 0 &&
            pattern.IndexOf('/') < 0;

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

        private bool IsSuffixMatch(ReadOnlySpan<char> path)
        {
            if (_basePrefix.Length > 0)
            {
                if (!path.StartsWith(_basePrefix, StringComparison.Ordinal)) return false;
                path = path[_basePrefix.Length..];
            }
            if (_anchored) return FirstSegment(path).EndsWith(_suffix, StringComparison.Ordinal);
            while (true)
            {
                var separator = path.IndexOf('/');
                var segment = separator < 0 ? path : path[..separator];
                if (segment.EndsWith(_suffix, StringComparison.Ordinal)) return true;
                if (separator < 0) return false;
                path = path[(separator + 1)..];
            }
        }

        private static ReadOnlySpan<char> FirstSegment(ReadOnlySpan<char> path)
        {
            var separator = path.IndexOf('/');
            return separator < 0 ? path : path[..separator];
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
