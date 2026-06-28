namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Worktrees;

internal sealed record GitWorktree(
    string Path,
    string? BranchName,
    bool IsCurrent,
    bool IsLocked,
    string LockReason);
