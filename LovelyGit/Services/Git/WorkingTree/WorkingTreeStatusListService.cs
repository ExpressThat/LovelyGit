using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeStatusListService
{
    private const int MaxNativeUntrackedFiles = 1_000;
    private const int MaxNativeUntrackedDirectories = 4_000;
    private readonly GitCliService _gitCliService;
    private readonly int _maxNativeUntrackedDirectories;

    public WorkingTreeStatusListService(GitCliService gitCliService)
        : this(gitCliService, MaxNativeUntrackedDirectories)
    {
    }

    internal WorkingTreeStatusListService(
        GitCliService gitCliService,
        int maxNativeUntrackedDirectories)
    {
        _gitCliService = gitCliService;
        _maxNativeUntrackedDirectories = maxNativeUntrackedDirectories;
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
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var trackedEntryCount = await GitIndexHeaderReader
            .ReadEntryCountAsync(paths.WorktreeGitDirectory, cancellationToken)
            .ConfigureAwait(false);
        if (trackedEntryCount is uint count
            && WorkingTreeStatusScanPolicy.ShouldSkipNativeScanBeforeRootTracking(count))
        {
            return null;
        }

        var objectFormat = await GitRepositoryDiscovery.ReadObjectFormatAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var scanner = new GitIndexStatusScanner();
        var fullScan = await scanner
            .ScanAsync(
                paths.WorktreeGitDirectory,
                paths.WorkTreeDirectory,
                objectFormat,
                cancellationToken,
                collectRootTracking: true)
            .ConfigureAwait(false);
        if (await HasStagedChangesAsync(
                paths.WorktreeGitDirectory,
                paths.GitDirectory,
                objectFormat,
                fullScan,
                cancellationToken)
                .ConfigureAwait(false))
        {
            return null;
        }

        var untracked = await FindUntrackedFilesAsync(
                paths.WorkTreeDirectory,
                paths.GitDirectory,
                fullScan.RootTrackedFiles,
                fullScan.RootTrackedDirectories,
                cancellationToken)
            .ConfigureAwait(false);
        if (!untracked.IsComplete)
        {
            return null;
        }

        var response = fullScan.Response;
        response.Untracked.AddRange(untracked.Files);
        fullScan.RootTrackedFiles.Clear();
        fullScan.RootTrackedDirectories.Clear();
        Sort(response.Unstaged);
        Sort(response.Untracked);
        Sort(response.Unmerged);
        return response;
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
