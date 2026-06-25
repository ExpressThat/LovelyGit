using System.Text;
using System.Text.RegularExpressions;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class GitIgnoreMatcher
{
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
