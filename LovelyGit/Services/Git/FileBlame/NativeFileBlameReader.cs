using ExpressThat.LovelyGit.Services.Git.FileHistory;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.FileBlame;

internal static partial class NativeFileBlameReader
{
    public const int DefaultMaximumCommits = 20_000;
    public const int DeepMaximumCommits = 100_000;
    public static readonly TimeSpan DefaultMaximumDuration = TimeSpan.FromMilliseconds(1_500);
    public static readonly TimeSpan DeepMaximumDuration = TimeSpan.FromSeconds(8);

    public static async Task<FileBlameResponse> ReadAsync(
        string repositoryPath,
        string path,
        string? startCommitHash,
        int maximumCommits,
        TimeSpan maximumDuration,
        CancellationToken cancellationToken)
    {
        var normalizedPath = FileHistoryPath.Normalize(path);
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var start = ResolveStart(repository, startCommitHash)
            ?? throw new InvalidDataException("Repository has no commits to blame.");
        var header = await repository.GetCommitTraversalHeaderAsync(start, cancellationToken)
            .ConfigureAwait(false);
        var file = await FindFileAsync(repository, header, normalizedPath, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new FileNotFoundException("File was not found at the selected commit.", normalizedPath);
        var source = BlameText.Decode(
            await repository.ReadBlobAsync(file.ObjectId, cancellationToken).ConfigureAwait(false));
        var active = new List<ActiveLine>(source.LineCount);
        for (var index = 0; index < source.LineCount; index++)
        {
            active.Add(new ActiveLine(index, index));
        }

        var attributions = new GitCommit?[source.LineCount];
        var current = new BlameState(start, normalizedPath, header, file, source);
        var traversal = await TraceAsync(
                repository,
                new BlameWorkItem(current, active),
                attributions,
                maximumCommits,
                maximumDuration,
                cancellationToken)
            .ConfigureAwait(false);
        var resolvedLineCount = attributions.Count(commit => commit != null);

        return new FileBlameResponse
        {
            Path = normalizedPath,
            StartCommitHash = start.Value,
            Content = source.Content,
            LineCount = source.LineCount,
            ScannedCommitCount = traversal.ScannedCommitCount,
            ResolvedLineCount = resolvedLineCount,
            IsPartial = traversal.IsPartial,
            Hunks = BuildHunks(attributions),
        };
    }

    private static async Task<GitTreeFile?> FindFileAsync(
        LovelyGitRepository repository,
        GitCommitTraversalHeader header,
        string path,
        CancellationToken cancellationToken) => header.TreeHash == null
        ? null
        : await repository.TryGetTreeFileAsync(header.TreeHash.Value, path, cancellationToken)
            .ConfigureAwait(false);

    private static GitObjectId? ResolveStart(LovelyGitRepository repository, string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) return repository.HeadTarget;
        return GitObjectId.TryParse(hash.Trim(), repository.ObjectFormat, out var id)
            ? id
            : throw new ArgumentException("The starting commit hash is invalid.", nameof(hash));
    }

    private static List<FileBlameHunk> BuildHunks(GitCommit?[] attributions)
    {
        var hunks = new List<FileBlameHunk>();
        var start = 0;
        while (start < attributions.Length)
        {
            var commit = attributions[start];
            var end = start + 1;
            while (end < attributions.Length && attributions[end]?.Hash == commit?.Hash) end++;
            hunks.Add(new FileBlameHunk
            {
                StartLine = start + 1,
                LineCount = end - start,
                Hash = commit?.Hash.Value,
                Author = commit?.AuthorName,
                Email = commit?.AuthorEmail,
                Date = commit?.AuthorUnixSeconds,
                Subject = commit?.Subject,
            });
            start = end;
        }

        return hunks;
    }

    private readonly record struct ActiveLine(int OriginalLine, int CurrentLine);
    private readonly record struct BlameWorkItem(BlameState State, List<ActiveLine> ActiveLines);
    private readonly record struct BlameState(
        GitObjectId Hash,
        string Path,
        GitCommitTraversalHeader Header,
        GitTreeFile File,
        BlameText Text);
    private readonly record struct BlameTraversalResult(int ScannedCommitCount, bool IsPartial);
}
