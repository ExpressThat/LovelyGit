using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeStatusListService
{
    private const int MaxNativeUntrackedFiles = 1_000;
    private const int MaxNativeUntrackedDirectories = 4_000;
    private readonly GitCliService _gitCliService;

    public WorkingTreeStatusListService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task<WorkingTreeChangesResponse> GetChangesAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var nativeResult = await TryGetNativeChangesAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        return nativeResult ?? await GetPorcelainChangesAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<WorkingTreeChangesResponse?> TryGetNativeChangesAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        using (repository)
        {
            var rootTracking = await GetRootTrackingAsync(repository, cancellationToken)
                .ConfigureAwait(false);
            var fastResponse = new WorkingTreeChangesResponse();
            var rootUntracked = await FindUntrackedFilesAsync(
                    repository.WorkTreeDirectory,
                    repository.GitDirectory,
                    rootTracking.RootTrackedFiles,
                    rootTracking.RootTrackedDirectories,
                    cancellationToken,
                    scanTrackedDirectories: false)
                .ConfigureAwait(false);
            if (!rootUntracked.IsComplete)
            {
                return null;
            }

            fastResponse.Untracked.AddRange(rootUntracked.Files);
            if (fastResponse.Untracked.Count > 0)
            {
                Sort(fastResponse.Untracked);
                return fastResponse;
            }

            var scanner = new GitIndexStatusScanner();
            var fullScan = await scanner
                .ScanAsync(
                    repository.GitDirectory,
                    repository.WorkTreeDirectory,
                    repository.ObjectFormat,
                    cancellationToken)
                .ConfigureAwait(false);
            if (await HasStagedChangesAsync(repository, fullScan, cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            var untracked = await FindUntrackedFilesAsync(
                    repository.WorkTreeDirectory,
                    repository.GitDirectory,
                    fullScan.RootTrackedFiles,
                    fullScan.RootTrackedDirectories,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!untracked.IsComplete)
            {
                return null;
            }

            fullScan.Response.Untracked.AddRange(untracked.Files);
            Sort(fullScan.Response.Unstaged);
            Sort(fullScan.Response.Untracked);
            Sort(fullScan.Response.Unmerged);
            return fullScan.Response;
        }
    }

    public async Task<WorkingTreeChangeSummaryResponse> GetSummaryAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var changes = await GetChangesAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        return new WorkingTreeChangeSummaryResponse
        {
            TotalCount = changes.TotalCount,
        };
    }

    private static async Task<bool> HasStagedChangesAsync(
        LovelyGitRepository repository,
        GitIndexStatusScan scan,
        CancellationToken cancellationToken)
    {
        if (scan.Response.Unmerged.Count > 0)
        {
            return false;
        }

        if (repository.HeadTarget == null)
        {
            return scan.RootTreeId != null;
        }

        var head = await repository.GetCommitAsync(repository.HeadTarget.Value, cancellationToken)
            .ConfigureAwait(false);
        return head.TreeHash == null || scan.RootTreeId != head.TreeHash;
    }

    private static async Task<GitIndexRootTracking> GetRootTrackingAsync(
        LovelyGitRepository repository,
        CancellationToken cancellationToken)
    {
        if (repository.HeadTarget == null)
        {
            return await new GitIndexRootTracker()
                .ReadAsync(repository.GitDirectory, repository.ObjectFormat, cancellationToken)
                .ConfigureAwait(false);
        }

        var head = await repository.GetCommitAsync(repository.HeadTarget.Value, cancellationToken)
            .ConfigureAwait(false);
        if (head.TreeHash == null)
        {
            return new GitIndexRootTracking([], []);
        }

        var files = new HashSet<string>(StringComparer.Ordinal);
        var directories = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in await repository.ReadRootTreeEntriesAsync(head.TreeHash.Value, cancellationToken)
            .ConfigureAwait(false))
        {
            if (entry.IsTree)
            {
                directories.Add(entry.Name);
            }
            else
            {
                files.Add(entry.Name);
            }
        }

        return new GitIndexRootTracking(files, directories);
    }

    private static WorkingTreeChangedFile Create(
        string path,
        string? oldPath,
        string status,
        WorkingTreeChangeGroup group) =>
        new()
        {
            Path = path,
            OldPath = oldPath,
            Status = status,
            Group = group,
        };

    private static void Sort(List<WorkingTreeChangedFile> files)
    {
        files.Sort((left, right) => string.Compare(left.Path, right.Path, StringComparison.Ordinal));
    }
}
