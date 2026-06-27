namespace ExpressThat.LovelyGit.Services.Git.Branches;

internal static class GitBranchNameValidator
{
    public static bool IsValidBranchName(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return false;
        }

        if (branchName.StartsWith('/') || branchName.EndsWith('/'))
        {
            return false;
        }

        if (branchName.EndsWith(".lock", StringComparison.Ordinal))
        {
            return false;
        }

        var previous = '\0';
        foreach (var current in branchName)
        {
            if (char.IsControl(current) || current is ' ' or '~' or '^' or ':' or '?' or '*' or '[' or '\\')
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

        return !branchName.Contains("@{", StringComparison.Ordinal)
            && !branchName.Contains("//", StringComparison.Ordinal)
            && !branchName.EndsWith('.');
    }
}
