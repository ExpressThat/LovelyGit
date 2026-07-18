using System.Globalization;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitPatchSeriesService
{
    private const int MaximumCharacters = 10_000_000;
    private readonly CommitPatchService _patchService;

    public CommitPatchSeriesService(CommitPatchService patchService)
    {
        _patchService = patchService;
    }

    public async Task<CommitPatchSeriesResponse> GetAsync(
        string repositoryPath,
        IReadOnlyList<GitObjectId> commitIds,
        CancellationToken cancellationToken)
    {
        using var repository = await LovelyGitRepository
            .OpenObjectDatabaseAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var output = new StringBuilder();
        var truncated = false;
        var binary = false;
        for (var index = 0; index < commitIds.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = commitIds[index];
            var commit = await repository.GetCommitAsync(id, cancellationToken).ConfigureAwait(false);
            var patch = await _patchService
                .GetCommitPatchAsync(repository, id, cancellationToken)
                .ConfigureAwait(false);
            if (patch.IsTruncated)
            {
                truncated = true;
                break;
            }
            if (patch.HasUnsupportedBinaryChanges)
            {
                binary = true;
                break;
            }
            AppendEntry(output, commit, patch.Patch, index + 1, commitIds.Count);
            if (output.Length > MaximumCharacters)
            {
                truncated = true;
                break;
            }
        }

        return new CommitPatchSeriesResponse
        {
            CommitCount = commitIds.Count,
            HasUnsupportedBinaryChanges = binary,
            IsTruncated = truncated,
            Patch = output.ToString(),
        };
    }

    private static void AppendEntry(
        StringBuilder output,
        GitCommit commit,
        string patch,
        int number,
        int total)
    {
        output.Append("From ").Append(commit.Hash).AppendLine(" Mon Sep 17 00:00:00 2001");
        output.Append("From: ").Append(commit.AuthorName).Append(" <")
            .Append(commit.AuthorEmail).AppendLine(">");
        output.Append("Date: ").AppendLine(
            DateTimeOffset.FromUnixTimeSeconds(commit.AuthorUnixSeconds)
                .ToString("ddd, dd MMM yyyy HH:mm:ss zzz", CultureInfo.InvariantCulture));
        output.Append("Subject: [PATCH ").Append(number).Append('/').Append(total).Append("] ")
            .AppendLine(commit.Subject);
        output.AppendLine();
        if (!string.IsNullOrWhiteSpace(commit.Body)) output.AppendLine(commit.Body.TrimEnd());
        output.AppendLine("---");
        output.Append(patch.TrimEnd('\r', '\n')).AppendLine()
            .AppendLine("-- ").AppendLine("LovelyGit").AppendLine();
    }
}
