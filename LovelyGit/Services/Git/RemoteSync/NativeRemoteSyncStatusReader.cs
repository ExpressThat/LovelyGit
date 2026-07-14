using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.BranchComparison;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.RemoteSync;

internal static class NativeRemoteSyncStatusReader
{
    public static async Task<RemoteSyncStatusResponse> ReadAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var objectFormat = await GitRepositoryDiscovery
            .ReadObjectFormatAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var branchName = await GitRefReader
            .ResolveHeadBranchNameAsync(paths.WorktreeGitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var localHash = await GitHeadReader
            .ResolveAsync(
                paths.WorktreeGitDirectory,
                paths.GitDirectory,
                objectFormat,
                cancellationToken)
            .ConfigureAwait(false);
        if (branchName == null || localHash == null)
        {
            return Calm(branchName, localHash);
        }

        var upstreams = await GitBranchUpstreamConfigReader
            .ReadAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        if (FindUpstream(upstreams, branchName) == null)
        {
            return Calm(branchName, localHash);
        }

        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        branchName = repository.CurrentBranchName;
        localHash = repository.HeadTarget;
        if (branchName == null || localHash == null)
        {
            return Calm(branchName, localHash);
        }

        var upstreamName = FindUpstream(upstreams, branchName);
        if (upstreamName == null)
        {
            return Calm(branchName, localHash);
        }

        if (!repository.TryGetBranch(upstreamName, out var upstream) || upstream == null)
        {
            return new RemoteSyncStatusResponse
            {
                BranchName = branchName,
                LocalHash = localHash.Value.ToString(),
                UpstreamName = upstreamName,
                HasUpstream = true,
            };
        }

        var counts = await NativeBranchComparisonReader.CountHistoryAsync(
            repository,
            localHash.Value,
            upstream.Target,
            cancellationToken).ConfigureAwait(false);
        return new RemoteSyncStatusResponse
        {
            BranchName = branchName,
            UpstreamName = upstreamName,
            LocalHash = localHash.Value.ToString(),
            UpstreamHash = upstream.Target.ToString(),
            AheadCount = counts.AheadCount,
            BehindCount = counts.BehindCount,
            HasUpstream = true,
            IsUpstreamAvailable = true,
            IsHistoryPartial = counts.IsPartial,
        };
    }

    private static string? FindUpstream(
        IEnumerable<GitBranchUpstream> upstreams,
        string branchName) =>
        upstreams.FirstOrDefault(upstream => string.Equals(
            upstream.BranchName,
            branchName,
            StringComparison.Ordinal))?.UpstreamName;

    private static RemoteSyncStatusResponse Calm(string? branchName, GitObjectId? localHash) =>
        new()
        {
            BranchName = branchName,
            LocalHash = localHash?.ToString(),
        };
}
