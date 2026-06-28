namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal static class GitRemoteNameValidator
{
    public static bool IsValidRemoteName(string remoteName)
    {
        if (string.IsNullOrWhiteSpace(remoteName)
            || remoteName != remoteName.Trim()
            || remoteName is "." or ".."
            || remoteName[0] == '-')
        {
            return false;
        }

        return !remoteName.Any(character =>
            char.IsControl(character)
            || char.IsWhiteSpace(character)
            || character is '/' or '\\'
            || character == ':');
    }
}
