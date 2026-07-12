using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.BranchComparison;

internal static partial class NativeBranchComparisonReader
{
    public static async Task<BranchComparisonResponse> ReadCommitsAsync(
        string repositoryPath,
        string currentCommitHash,
        string targetCommitHash,
        CancellationToken cancellationToken)
    {
        var normalizedCurrent = NormalizeCommitHash(currentCommitHash, nameof(currentCommitHash));
        var normalizedTarget = NormalizeCommitHash(targetCommitHash, nameof(targetCommitHash));
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var current = ParseCommitId(repository, normalizedCurrent, nameof(currentCommitHash));
        var target = ParseCommitId(repository, normalizedTarget, nameof(targetCommitHash));
        await Task.WhenAll(
            repository.GetCommitAsync(current, cancellationToken),
            repository.GetCommitAsync(target, cancellationToken)).ConfigureAwait(false);
        return await BuildResponseAsync(
            repository,
            current,
            target,
            ShortHash(current),
            ShortHash(target),
            cancellationToken).ConfigureAwait(false);
    }

    private static GitObjectId ParseCommitId(
        LovelyGitRepository repository,
        string value,
        string parameterName)
    {
        if (!GitObjectId.TryParse(value, repository.ObjectFormat, out var id))
        {
            throw new ArgumentException("Commit hash is invalid for this repository.", parameterName);
        }
        return id;
    }

    private static string NormalizeCommitHash(string value, string parameterName)
    {
        var normalized = value.Trim();
        if ((normalized.Length is not 40 and not 64)
            || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("Commit hash is invalid.", parameterName);
        }
        return normalized;
    }

    private static string ShortHash(GitObjectId id) => id.ToString()[..7];
}
