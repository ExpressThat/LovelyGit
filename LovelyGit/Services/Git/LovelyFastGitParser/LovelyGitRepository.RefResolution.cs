namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository
{
    public bool TryResolveRefTarget(string name, out GitObjectId target)
    {
        var normalized = name.Trim();
        if (normalized.Equals("HEAD", StringComparison.OrdinalIgnoreCase)
            && HeadTarget is { } head)
        {
            target = head;
            return true;
        }

        if (_refsByFullName.TryGetValue(normalized, out var full)
            || _refsByFullName.TryGetValue($"refs/heads/{normalized}", out full)
            || _refsByFullName.TryGetValue($"refs/remotes/{normalized}", out full)
            || _refsByFullName.TryGetValue($"refs/tags/{normalized}", out full))
        {
            target = full.Target;
            return true;
        }

        target = default;
        return false;
    }
}
