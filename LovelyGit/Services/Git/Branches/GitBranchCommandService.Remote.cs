using ExpressThat.LovelyGit.Services.Git.Cli;

namespace ExpressThat.LovelyGit.Services.Git.Branches;

internal sealed partial class GitBranchCommandService
{
    public Task PushBranchAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken) =>
        PushBranchAsync(repositoryPath, "origin", branchName, cancellationToken);

    public Task PushBranchAsync(
        string repositoryPath,
        string remoteName,
        string branchName,
        CancellationToken cancellationToken)
    {
        var remote = NormalizeRemoteName(remoteName);
        var branch = NormalizeBranchName(branchName);
        return RunAsync(
            repositoryPath,
            "Push branch",
            ["push", "--set-upstream", remote, $"refs/heads/{branch}:refs/heads/{branch}"],
            "Check authentication, remote permissions, and whether the remote branch has diverged.",
            cancellationToken);
    }

    public Task PullBranchAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            "Pull branch",
            ["pull", "--ff-only", "origin", NormalizeBranchName(branchName)],
            "Resolve local divergence or choose a merge/rebase pull strategy.",
            cancellationToken);

    public Task DeleteRemoteBranchAsync(
        string repositoryPath,
        string remoteBranchName,
        CancellationToken cancellationToken)
    {
        var (remote, branch) = NormalizeRemoteBranchName(remoteBranchName);
        return RunAsync(
            repositoryPath,
            "Delete remote branch",
            ["push", "--delete", remote, $"refs/heads/{branch}"],
            "Check authentication, remote permissions, and whether the remote branch still exists.",
            cancellationToken);
    }

    public Task SetBranchUpstreamAsync(
        string repositoryPath,
        string branchName,
        string upstreamName,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            "Set branch upstream",
            ["branch", "--set-upstream-to", NormalizeTrackingName(upstreamName), NormalizeBranchName(branchName)],
            "Verify the remote branch exists and try again.",
            cancellationToken);

    public Task UnsetBranchUpstreamAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            "Unset branch upstream",
            ["branch", "--unset-upstream", NormalizeBranchName(branchName)],
            "Verify the branch currently has an upstream.",
            cancellationToken);

    private static string NormalizeRemoteName(string remoteName) =>
        GitRemoteNameValidator.IsValidRemoteName(remoteName)
            ? remoteName.Trim()
            : throw new ArgumentException("Remote name is not valid.", nameof(remoteName));

    private static string NormalizeTrackingName(string upstreamName)
    {
        var normalized = upstreamName.Trim();
        var slashIndex = normalized.IndexOf('/');
        if (slashIndex <= 0 || slashIndex == normalized.Length - 1 ||
            !GitRemoteNameValidator.IsValidRemoteName(normalized[..slashIndex]) ||
            !GitBranchNameValidator.IsValidBranchName(normalized[(slashIndex + 1)..]))
        {
            throw new ArgumentException("Upstream branch name is not valid.", nameof(upstreamName));
        }

        return normalized;
    }

    private static (string Remote, string Branch) NormalizeRemoteBranchName(
        string remoteBranchName)
    {
        var normalized = remoteBranchName.Trim();
        var slashIndex = normalized.IndexOf('/');
        if (slashIndex <= 0 || slashIndex == normalized.Length - 1 ||
            !GitRemoteNameValidator.IsValidRemoteName(normalized[..slashIndex]) ||
            !GitBranchNameValidator.IsValidBranchName(normalized[(slashIndex + 1)..]))
        {
            throw new ArgumentException("Remote branch name is not valid.", nameof(remoteBranchName));
        }

        return (normalized[..slashIndex], normalized[(slashIndex + 1)..]);
    }
}
