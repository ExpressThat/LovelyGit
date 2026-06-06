using ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitDetailsBuilder
{
    private readonly LovelyGitRepository _repository;
    private readonly CommitChangeDetector _changeDetector;

    public CommitDetailsBuilder(LovelyGitRepository repository)
    {
        _repository = repository;
        _changeDetector = new CommitChangeDetector(new BlobLineAnalyzer(repository));
    }

    public async Task<CommitDetailsResponse> BuildAsync(
        GitCommit commit,
        GitCommit? firstParent,
        CancellationToken cancellationToken)
    {
        var comparison = await _repository.GetChangedTreeFilesAsync(firstParent?.TreeHash, commit.TreeHash, cancellationToken)
            .ConfigureAwait(false);
        var changedFiles = await _changeDetector
            .BuildChangedFilesAsync(comparison.ParentFiles, comparison.CurrentFiles, cancellationToken)
            .ConfigureAwait(false);

        return new CommitDetailsResponse
        {
            Hash = commit.Hash.ToString(),
            Parents = commit.ParentHashes.Select(parent => parent.ToString()).ToList(),
            Author = string.IsNullOrWhiteSpace(commit.AuthorName) ? "unknown" : commit.AuthorName,
            Email = commit.AuthorEmail,
            Date = commit.AuthorUnixSeconds,
            Subject = commit.Subject,
            Body = commit.Body,
            Message = commit.Body.Trim('\r', '\n'),
            Branches = commit.Branches.ToList(),
            Tags = commit.Tags.ToList(),
            Stats = new CommitStats
            {
                Additions = changedFiles.Aggregate(0u, (total, file) => total + file.Additions),
                Deletions = changedFiles.Aggregate(0u, (total, file) => total + file.Deletions),
            },
            ChangedFiles = changedFiles,
        };
    }
}
