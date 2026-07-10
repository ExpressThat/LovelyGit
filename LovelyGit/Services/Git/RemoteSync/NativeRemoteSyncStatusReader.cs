using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.BranchComparison;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.RemoteSync;

internal static class NativeRemoteSyncStatusReader
{
    public static async Task<RemoteSyncStatusResponse> ReadAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var branchName = repository.CurrentBranchName;
        var localHash = repository.HeadTarget;
        if (branchName == null || localHash == null)
        {
            return new RemoteSyncStatusResponse
            {
                BranchName = branchName,
                LocalHash = localHash?.ToString(),
            };
        }

        var upstreams = await GitBranchUpstreamConfigReader
            .ReadAsync(repository.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var upstreamName = upstreams
            .FirstOrDefault(upstream => string.Equals(
                upstream.BranchName,
                branchName,
                StringComparison.Ordinal))
            ?.UpstreamName;
        if (upstreamName == null)
        {
            return new RemoteSyncStatusResponse
            {
                BranchName = branchName,
                LocalHash = localHash.Value.ToString(),
            };
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
}
