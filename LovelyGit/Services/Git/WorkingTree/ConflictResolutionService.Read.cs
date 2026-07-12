using System.Security.Cryptography;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionService
{
    private const int MaxTextBytes = 4 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public async Task<ConflictResolutionResponse> ReadAsync(
        string repositoryPath,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        path = WorkingTreePath.NormalizeRelative(path);
        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var entries = await ReadConflictEntriesAsync(repository, path, cancellationToken).ConfigureAwait(false);
        var baseVersion = await ReadVersionAsync(repository, entries.GetValueOrDefault(1), cancellationToken)
            .ConfigureAwait(false);
        var ours = await ReadVersionAsync(repository, entries.GetValueOrDefault(2), cancellationToken)
            .ConfigureAwait(false);
        var theirs = await ReadVersionAsync(repository, entries.GetValueOrDefault(3), cancellationToken)
            .ConfigureAwait(false);
        var resultPath = WorkingTreePath.Resolve(repository.WorkTreeDirectory, path);
        var result = await ReadWorktreeVersionAsync(resultPath, cancellationToken).ConfigureAwait(false);
        var currentSource = CreateCurrentSource(repository, entries.GetValueOrDefault(2));
        var incomingSource = await CreateIncomingSourceAsync(
            repository,
            repositoryPaths.WorktreeGitDirectory,
            entries.GetValueOrDefault(3),
            cancellationToken).ConfigureAwait(false);
        var canBuildTextMerge = baseVersion.Text != null &&
            ours.Text != null &&
            theirs.Text != null &&
            result.Text != null;

        var response = new ConflictResolutionResponse
        {
            Path = path,
            WorktreeFingerprint = await ConflictFingerprintAsync(resultPath, entries, cancellationToken)
                .ConfigureAwait(false),
            Base = baseVersion,
            Ours = ours,
            Theirs = theirs,
            Result = result,
            CurrentSource = currentSource,
            IncomingSource = incomingSource,
            Hunks = canBuildTextMerge
                ? ConflictHunkBuilder.Build(baseVersion.Text!, ours.Text!, theirs.Text!, result.Text!)
                : new List<ConflictHunk>(),
            CurrentComparison = canBuildTextMerge
                ? BuildBaseComparison(path, ignoreWhitespace, baseVersion.Text!, ours.Text!)
                : null,
            IncomingComparison = canBuildTextMerge
                ? BuildBaseComparison(path, ignoreWhitespace, baseVersion.Text!, theirs.Text!)
                : null,
        };
        ConflictComparisonPayloadBuilder.Compact(response.CurrentComparison);
        ConflictComparisonPayloadBuilder.Compact(response.IncomingComparison);
        ConflictTextPayloadBuilder.Compact(response);
        return response;
    }

    private static CommitFileDiffResponse BuildBaseComparison(
        string path,
        bool ignoreWhitespace,
        string baseText,
        string sourceText)
    {
        return WorkingTreeChangeService.BuildDiffResponse(
            "CONFLICT",
            path,
            "Unmerged",
            CommitDiffViewMode.SideBySide,
            ignoreWhitespace,
            Encoding.UTF8.GetBytes(baseText),
            Encoding.UTF8.GetBytes(sourceText),
            compact: false);
    }

    private static async Task<Dictionary<int, GitIndexEntry>> ReadConflictEntriesAsync(
        LovelyGitRepository repository,
        string path,
        CancellationToken cancellationToken)
    {
        var entries = await new GitIndexReader()
            .ReadAsync(repository.GitDirectory, repository.ObjectFormat, cancellationToken)
            .ConfigureAwait(false);
        var conflict = entries
            .Where(entry => entry.Path == path && entry.Stage is >= 1 and <= 3)
            .ToDictionary(entry => entry.Stage);
        if (conflict.Count == 0)
        {
            throw new InvalidOperationException("This file no longer has an unresolved conflict.");
        }

        return conflict;
    }

    private static async Task<ConflictFileVersion> ReadVersionAsync(
        LovelyGitRepository repository,
        GitIndexEntry? entry,
        CancellationToken cancellationToken)
    {
        if (entry == null)
        {
            return new ConflictFileVersion();
        }

        if (entry.FileSize > MaxTextBytes)
        {
            return new ConflictFileVersion { Exists = true, IsTooLarge = true, SizeBytes = entry.FileSize };
        }

        var bytes = await repository.ReadBlobAsync(entry.ObjectId, cancellationToken).ConfigureAwait(false);
        return CreateVersion(bytes);
    }

    private static async Task<ConflictFileVersion> ReadWorktreeVersionAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return new ConflictFileVersion();
        }

        var info = new FileInfo(path);
        if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidOperationException("Symbolic-link conflicts are not supported yet.");
        }

        if (info.Length > MaxTextBytes)
        {
            return new ConflictFileVersion { Exists = true, IsTooLarge = true, SizeBytes = info.Length };
        }

        return CreateVersion(await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false));
    }

    private static ConflictFileVersion CreateVersion(byte[] bytes)
    {
        var isBinary = WorkingTreeChangeService.IsBinary(bytes);
        string? text = null;
        if (!isBinary)
        {
            try
            {
                text = StrictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                isBinary = true;
            }
        }

        return new ConflictFileVersion
        {
            Exists = true,
            IsBinary = isBinary,
            SizeBytes = bytes.Length,
            Text = text,
        };
    }

    private static async Task<string> FingerprintAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return "missing";
        }

        await using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }

    private static async Task<string> ConflictFingerprintAsync(
        string path,
        IReadOnlyDictionary<int, GitIndexEntry> entries,
        CancellationToken cancellationToken)
    {
        var worktreeFingerprint = await FingerprintAsync(path, cancellationToken).ConfigureAwait(false);
        var descriptor = string.Join(
            ';',
            entries.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}:{entry.Value.ObjectId}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes($"{worktreeFingerprint}|{descriptor}")));
    }
}
