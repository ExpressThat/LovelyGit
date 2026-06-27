namespace ExpressThat.LovelyGit.Services.Git.Tags;

internal static class GitTagNameValidator
{
    public static bool IsValidTagName(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName) || tagName is "@" || tagName[0] == '-')
        {
            return false;
        }

        if (tagName.StartsWith('/') || tagName.EndsWith('/'))
        {
            return false;
        }

        if (tagName.StartsWith("refs/", StringComparison.Ordinal)
            || tagName.EndsWith(".lock", StringComparison.Ordinal))
        {
            return false;
        }

        var previous = '\0';
        foreach (var current in tagName)
        {
            if (char.IsControl(current)
                || current is ' ' or '~' or '^' or ':' or '?' or '*' or '[' or '\\')
            {
                return false;
            }

            if (current == '/' && previous == '/')
            {
                return false;
            }

            if (current == '.' && previous == '.')
            {
                return false;
            }

            previous = current;
        }

        return !tagName.Contains("@{", StringComparison.Ordinal)
            && !tagName.EndsWith('.');
    }
}
