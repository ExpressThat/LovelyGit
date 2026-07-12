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
        GitCommit? comparisonParent,
        CancellationToken cancellationToken)
    {
        var comparison = await _repository.GetChangedTreeFilesAsync(comparisonParent?.TreeHash, commit.TreeHash, cancellationToken)
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
            Branches = BuildRefNames(commit, includeBranches: true),
            Tags = BuildRefNames(commit, includeBranches: false),
            Stats = new CommitStats
            {
                Additions = changedFiles.Aggregate(0u, (total, file) => total + file.Additions),
                Deletions = changedFiles.Aggregate(0u, (total, file) => total + file.Deletions),
            },
            ChangedFiles = changedFiles,
            SignatureKind = CommitGraphCommitMapper.MapSignatureKind(commit.SignatureKind),
        };
    }

    internal static List<string> BuildRefNames(GitCommit commit, bool includeBranches)
    {
        List<string>? names = null;
        foreach (var reference in commit.Refs)
        {
            var matches = includeBranches
                ? reference.Kind is GitRefKind.Head or GitRefKind.Remote
                : reference.Kind == GitRefKind.Tag;
            if (matches)
            {
                (names ??= new List<string>()).Add(reference.Name);
            }
        }

        return names ?? [];
    }
}
