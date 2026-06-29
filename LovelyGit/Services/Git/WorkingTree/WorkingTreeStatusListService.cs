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
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var objectFormat = await GitRepositoryDiscovery.ReadObjectFormatAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var rootTracking = await new GitIndexRootTracker()
            .ReadAsync(paths.GitDirectory, objectFormat, cancellationToken)
            .ConfigureAwait(false);
        var fastResponse = new WorkingTreeChangesResponse();
        var rootUntracked = await FindUntrackedFilesAsync(
                paths.WorkTreeDirectory,
                paths.GitDirectory,
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

        if (WorkingTreeStatusScanPolicy.ShouldUseCompleteFallbackForDeepUntrackedScan(
                rootTracking.RootTrackedDirectories.Count,
                rootTracking.TrackedEntryCount))
        {
            return null;
        }

        var scanner = new GitIndexStatusScanner();
        var fullScan = await scanner
            .ScanAsync(
                paths.GitDirectory,
                paths.WorkTreeDirectory,
                objectFormat,
                cancellationToken,
                collectRootTracking: false)
            .ConfigureAwait(false);
        if (await HasStagedChangesAsync(paths.GitDirectory, objectFormat, fullScan, cancellationToken)
                .ConfigureAwait(false))
        {
            return null;
        }

        var untracked = await FindUntrackedFilesAsync(
                paths.WorkTreeDirectory,
                paths.GitDirectory,
                rootTracking.RootTrackedFiles,
                rootTracking.RootTrackedDirectories,
                cancellationToken)
            .ConfigureAwait(false);

        var response = fullScan.Response;
        response.Untracked.AddRange(untracked.Files);
        fullScan.RootTrackedFiles.Clear();
        fullScan.RootTrackedDirectories.Clear();
        GitIndexMemory.ReleaseLargeAllocations();
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

    private static async Task<bool> HasStagedChangesAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        GitIndexStatusScan scan,
        CancellationToken cancellationToken)
    {
        if (scan.Response.Unmerged.Count > 0)
        {
            return false;
        }

        var headTreeId = await ReadHeadTreeIdAsync(gitDirectory, objectFormat, cancellationToken)
            .ConfigureAwait(false);
        if (headTreeId == null)
        {
            return scan.RootTreeId != null;
        }

        return scan.RootTreeId != headTreeId;
    }

    private static async Task<GitObjectId?> ReadHeadTreeIdAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var headTarget = await ReadHeadTargetAsync(gitDirectory, objectFormat, cancellationToken)
            .ConfigureAwait(false);
        if (headTarget == null)
        {
            return null;
        }

        using var objectStore = new GitObjectStore(gitDirectory, objectFormat);
        var data = await objectStore.ReadObjectAsync(headTarget.Value, cancellationToken)
            .ConfigureAwait(false);
        return data.Kind == GitObjectKind.Commit
            ? GitObjectParsers.ParseCommit(headTarget.Value, data.Data).TreeHash
            : null;
    }

    private static async Task<GitObjectId?> ReadHeadTargetAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var headPath = Path.Combine(gitDirectory, "HEAD");
        if (!File.Exists(headPath))
        {
            return null;
        }

        var text = (await File.ReadAllTextAsync(headPath, cancellationToken).ConfigureAwait(false)).Trim();
        const string refPrefix = "ref:";
        if (!text.StartsWith(refPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return GitObjectId.TryParse(text, objectFormat, out var detachedId) ? detachedId : null;
        }

        var refName = text.AsSpan(refPrefix.Length).Trim().ToString();
        return await ReadRefTargetAsync(gitDirectory, objectFormat, refName, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<GitObjectId?> ReadRefTargetAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        string refName,
        CancellationToken cancellationToken)
    {
        var looseRefPath = Path.Combine(gitDirectory, refName.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(looseRefPath))
        {
            var text = (await File.ReadAllTextAsync(looseRefPath, cancellationToken).ConfigureAwait(false)).Trim();
            return GitObjectId.TryParse(text, objectFormat, out var id) ? id : null;
        }

        return ReadPackedRefTarget(gitDirectory, objectFormat, refName, cancellationToken);
    }

    private static GitObjectId? ReadPackedRefTarget(
        string gitDirectory,
        GitObjectFormat objectFormat,
        string refName,
        CancellationToken cancellationToken)
    {
        var packedRefsPath = Path.Combine(gitDirectory, "packed-refs");
        if (!File.Exists(packedRefsPath))
        {
            return null;
        }

        foreach (var line in File.ReadLines(packedRefsPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (line.Length == 0 || line[0] == '#' || line[0] == '^')
            {
                continue;
            }

            var spaceIndex = line.IndexOf(' ');
            if (spaceIndex <= 0
                || !line.AsSpan(spaceIndex + 1).SequenceEqual(refName)
                || !GitObjectId.TryParse(line.AsSpan(0, spaceIndex), objectFormat, out var id))
            {
                continue;
            }

            return id;
        }

        return null;
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
