namespace ExpressThat.LovelyGit.Services.Git.Branches;

internal static class GitBranchNameValidator
{
    private static readonly char[] InvalidCharacters = [' ', '~', '^', ':', '?', '*', '[', '\\'];

    public static bool IsValidBranchName(string? branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return false;
        }

        var value = branchName.Trim();
        if (value.Length > 255 || value.StartsWith('-') || value.StartsWith('/') ||
            value.EndsWith('/') || value.EndsWith('.') ||
            value.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase) ||
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
