using System.Text;
using System.Text.RegularExpressions;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class GitIgnoreMatcher
{
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

}
