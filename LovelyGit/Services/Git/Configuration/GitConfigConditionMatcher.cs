using System.IO.Enumeration;

namespace ExpressThat.LovelyGit.Services.Git.Configuration;

internal static class GitConfigConditionMatcher
{
    public static bool Matches(
        string condition,
        string gitDirectory,
        string? branchName,
        string? homeDirectory,
        string includingConfigPath)
    {
        var separator = condition.IndexOf(':');
        if (separator <= 0)
        {
            return false;
        }

        var kind = condition[..separator];
        var pattern = condition[(separator + 1)..];
        if (kind.Equals("onbranch", StringComparison.OrdinalIgnoreCase))
        {
            return branchName is not null && MatchesGlob(pattern, branchName, false);
        }

        if (!kind.Equals("gitdir", StringComparison.OrdinalIgnoreCase) &&
            !kind.Equals("gitdir/i", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var expanded = ExpandPath(pattern, homeDirectory, includingConfigPath);
        if (!Path.IsPathRooted(expanded) && !expanded.StartsWith("**/", StringComparison.Ordinal))
        {
            expanded = "**/" + expanded;
        }

        if (expanded.EndsWith('/') || expanded.EndsWith(Path.DirectorySeparatorChar))
        {
            expanded += "**";
        }

        return MatchesGlob(
            Normalize(expanded),
            Normalize(Path.GetFullPath(gitDirectory)) + "/",
            kind.Equals("gitdir/i", StringComparison.OrdinalIgnoreCase));
    }

    public static string ResolveIncludePath(
        string value,
        string? homeDirectory,
        string includingConfigPath)
    {
        var expanded = ExpandPath(value, homeDirectory, includingConfigPath);
        return Path.GetFullPath(expanded);
    }

    private static string ExpandPath(
        string value,
        string? homeDirectory,
        string includingConfigPath)
    {
        if (value.Equals("~", StringComparison.Ordinal) && homeDirectory is not null)
        {
            return homeDirectory;
        }

        if ((value.StartsWith("~/", StringComparison.Ordinal) ||
             value.StartsWith("~\\", StringComparison.Ordinal)) && homeDirectory is not null)
        {
            return Path.Combine(homeDirectory, value[2..]);
        }

        if (value.StartsWith("./", StringComparison.Ordinal) ||
            value.StartsWith(".\\", StringComparison.Ordinal))
        {
            var parent = Path.GetDirectoryName(includingConfigPath) ?? string.Empty;
            return Path.Combine(parent, value[2..]);
        }

        return value;
    }

    private static bool MatchesGlob(string pattern, string value, bool ignoreCase) =>
        FileSystemName.MatchesSimpleExpression(pattern, value, ignoreCase);

    private static string Normalize(string value) => value.Replace('\\', '/');
}
