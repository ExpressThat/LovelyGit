using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitSearch;

internal static partial class NativeCommitSearchReader
{
    public const int DefaultMaximumCommits = 100_000;
    public const int DefaultResponsiveMatchScanCount = 64;
    public const int DefaultResultLimit = 50;
    public static readonly TimeSpan DefaultMaximumDuration = TimeSpan.FromMilliseconds(300);
    public const int DeepMaximumCommits = 500_000;
    public static readonly TimeSpan DeepMaximumDuration = TimeSpan.FromSeconds(9);
    internal const int MaximumResultLimit = 100;

    public static async Task<CommitSearchResponse> SearchAsync(
        string repositoryPath,
        string query,
        string author,
        string scope,
        long? afterUnixSeconds,
        long? beforeUnixSeconds,
        int limit,
        int maximumCommits,
        TimeSpan maximumDuration,
        CancellationToken cancellationToken,
        int? responsiveMatchScanCount = null)
    {
        using var session = await NativeCommitSearchSession.OpenAsync(
            repositoryPath,
            query,
            author,
            scope,
            afterUnixSeconds,
            beforeUnixSeconds,
            limit,
            cancellationToken).ConfigureAwait(false);
        return await session.ScanAsync(
            maximumCommits,
            maximumDuration,
            cancellationToken,
            responsiveMatchScanCount).ConfigureAwait(false);
    }

    internal static async Task<CommitSearchResult?> TryResolveHashAsync(
        LovelyGitRepository repository,
        string query,
        CancellationToken cancellationToken)
    {
        if (query.Length < 7 || query.Any(character => !Uri.IsHexDigit(character))) return null;
        var id = await repository.ResolveUniqueObjectPrefixAsync(query, cancellationToken)
            .ConfigureAwait(false);
        if (id == null) return null;
        try
        {
            return ToResult(
                await repository.GetCommitAsync(id.Value, cancellationToken).ConfigureAwait(false),
                query);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

}
