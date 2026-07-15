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
        var head = await GitHeadReader
            .ReadAsync(
                paths.WorktreeGitDirectory,
                paths.GitDirectory,
                objectFormat,
                cancellationToken)
            .ConfigureAwait(false);
        var branchName = head.BranchName;
        var localHash = head.Target;
        if (branchName == null || localHash == null)
        {
            return Calm(branchName, localHash);
        }

        var upstreams = await GitBranchUpstreamConfigReader
            .ReadAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var upstream = FindUpstream(upstreams, branchName);
        if (upstream == null)
        {
            return Calm(branchName, localHash);
        }

        var upstreamTarget = await GitHeadReader.ResolveRefAsync(
                paths.GitDirectory,
                upstream.RefName,
                objectFormat,
                cancellationToken)
            .ConfigureAwait(false);
        if (upstreamTarget == null)
        {
            return new RemoteSyncStatusResponse
            {
                BranchName = branchName,
                LocalHash = localHash.Value.ToString(),
                UpstreamName = upstream.UpstreamName,
                HasUpstream = true,
            };
        }

        using var repository = await LovelyGitRepository
            .OpenObjectDatabaseAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var counts = await NativeBranchComparisonReader.CountHistoryAsync(
            repository,
            localHash.Value,
            upstreamTarget.Value,
            cancellationToken).ConfigureAwait(false);
        return new RemoteSyncStatusResponse
        {
            BranchName = branchName,
            UpstreamName = upstream.UpstreamName,
            LocalHash = localHash.Value.ToString(),
            UpstreamHash = upstreamTarget.Value.ToString(),
            AheadCount = counts.AheadCount,
            BehindCount = counts.BehindCount,
            HasUpstream = true,
            IsUpstreamAvailable = true,
            IsHistoryPartial = counts.IsPartial,
        };
    }

    private static GitBranchUpstream? FindUpstream(
        IEnumerable<GitBranchUpstream> upstreams,
        string branchName) =>
        upstreams.FirstOrDefault(upstream => string.Equals(
            upstream.BranchName,
            branchName,
            StringComparison.Ordinal));

    private static RemoteSyncStatusResponse Calm(string? branchName, GitObjectId? localHash) =>
        new()
        {
            BranchName = branchName,
            LocalHash = localHash?.ToString(),
        };
}
