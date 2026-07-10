namespace ExpressThat.LovelyGit.Services.Git.Tags;

internal static class GitTagNameValidator
{
    private static readonly char[] InvalidCharacters = [' ', '~', '^', ':', '?', '*', '[', '\\'];

    public static bool IsValidTagName(string? tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return false;
        }

        var value = tagName.Trim();
        if (value.Length > 255 || value is "@" || value.StartsWith('-') ||
            value.StartsWith('/') || value.EndsWith('/') || value.EndsWith('.') ||
            value.StartsWith("refs/tags/", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".lock", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("..", StringComparison.Ordinal) ||
            value.Contains("//", StringComparison.Ordinal) ||
            value.Contains("@{", StringComparison.Ordinal))
        {
            return false;
        }

        if (value.Any(character => char.IsControl(character) ||
            InvalidCharacters.Contains(character)))
        {
            return false;
        }

        return value.Split('/').All(part => part.Length > 0 && !part.StartsWith('.'));
    }
}
