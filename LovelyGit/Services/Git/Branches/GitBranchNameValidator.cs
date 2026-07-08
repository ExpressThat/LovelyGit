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

        var trimmed = branchName.Trim();
        return trimmed == branchName
            && !trimmed.StartsWith('/')
            && !trimmed.EndsWith('/')
            && !trimmed.EndsWith('.')
            && !trimmed.EndsWith(".lock", StringComparison.Ordinal)
            && !trimmed.Contains("//", StringComparison.Ordinal)
            && !trimmed.Contains("..", StringComparison.Ordinal)
            && !trimmed.Contains("@{", StringComparison.Ordinal)
            && trimmed.IndexOfAny(InvalidCharacters) < 0;
    }

    public static string RequireValidBranchName(string branchName, string parameterName)
    {
        if (!IsValidBranchName(branchName))
        {
            throw new ArgumentException("Branch name is not valid.", parameterName);
        }

        return branchName.Trim();
    }
}
