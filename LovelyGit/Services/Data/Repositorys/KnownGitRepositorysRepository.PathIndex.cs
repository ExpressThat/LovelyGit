using ExpressThat.LovelyGit.Services.Data.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

public partial class KnownGitRepositorysRepository
{
    private const string CompletePathIndexId = "$complete-v1";
    private readonly SemaphoreSlim _pathIndexGate = new(1, 1);

    public async Task<KnownGitRepository?> FindByPathAsync(string repositoryPath)
    {
        var pathKey = CreatePathKey(repositoryPath);
        var indexed = await FindIndexedByPathAsync(pathKey);
        if (indexed.Repository != null)
        {
            return indexed.Repository;
        }

        var complete = await _appDbContext.KnownGitRepositoryPaths.FindByIdAsync(
            CompletePathIndexId);
        if (complete != null && !indexed.IsStale)
        {
            return null;
        }

        await RebuildPathIndexAsync(indexed.IsStale);
        return (await FindIndexedByPathAsync(pathKey)).Repository;
    }

    internal Task<int> EnsurePathIndexAsync() => RebuildPathIndexAsync(force: false);

    private async Task<(KnownGitRepository? Repository, bool IsStale)> FindIndexedByPathAsync(
        string pathKey)
    {
        var pathEntry = await _appDbContext.KnownGitRepositoryPaths.FindByIdAsync(pathKey);
        if (pathEntry == null)
        {
            return (null, false);
        }

        var repository = await TryFindByIdAsync(pathEntry.RepositoryId);
        return repository?.Path is { } path && CreatePathKey(path) == pathKey
            ? (repository, false)
            : (null, true);
    }

    private async Task<int> RebuildPathIndexAsync(bool force)
    {
        await _pathIndexGate.WaitAsync();
        try
        {
            if (!force && await HasCompletePathIndexAsync())
            {
                return 0;
            }

            var repositories = await GetAllAsync();
            var existingEntries = await _appDbContext.KnownGitRepositoryPaths
                .AsQueryable()
                .ToListAsync();
            var existingByPath = existingEntries.ToDictionary(entry => entry.Id);
            var desiredByPath = CreateDesiredPathEntries(repositories);
            using var transaction = _appDbContext.BeginTransaction();
            using var retention = BLiteTransactionRetention.Track(transaction);
            await SynchronizePathEntriesAsync(existingByPath, desiredByPath, transaction);
            await _appDbContext.SaveChangesAsync(transaction);
            return desiredByPath.Count;
        }
        finally
        {
            _pathIndexGate.Release();
        }
    }

    private async Task SynchronizePathEntriesAsync(
        Dictionary<string, KnownGitRepositoryPath> existing,
        Dictionary<string, KnownGitRepositoryPath> desired,
        BLite.Core.Transactions.ITransaction transaction)
    {
        foreach (var (path, entry) in desired)
        {
            if (!existing.TryGetValue(path, out var current))
            {
                await _appDbContext.KnownGitRepositoryPaths.InsertAsync(entry, transaction);
            }
            else if (current.RepositoryId != entry.RepositoryId)
            {
                await _appDbContext.KnownGitRepositoryPaths.UpdateAsync(entry, transaction);
            }
        }

        foreach (var path in existing.Keys)
        {
            if (path != CompletePathIndexId && !desired.ContainsKey(path))
            {
                await _appDbContext.KnownGitRepositoryPaths.DeleteAsync(path, transaction);
            }
        }

        if (!existing.ContainsKey(CompletePathIndexId))
        {
            await _appDbContext.KnownGitRepositoryPaths.InsertAsync(new KnownGitRepositoryPath
            {
                Id = CompletePathIndexId,
                RepositoryId = Guid.Empty,
            }, transaction);
        }
    }

    private async ValueTask<bool> HasCompletePathIndexAsync() =>
        await _appDbContext.KnownGitRepositoryPaths.FindByIdAsync(CompletePathIndexId) != null;

    private static Dictionary<string, KnownGitRepositoryPath> CreateDesiredPathEntries(
        IEnumerable<KnownGitRepository> repositories)
    {
        var desired = new Dictionary<string, KnownGitRepositoryPath>(StringComparer.Ordinal);
        foreach (var repository in repositories)
        {
            if (repository.Path is not { } path)
            {
                continue;
            }

            var pathKey = CreatePathKey(path);
            desired.TryAdd(pathKey, new KnownGitRepositoryPath
            {
                Id = pathKey,
                RepositoryId = repository.Id,
            });
        }

        return desired;
    }

    private static string CreatePathKey(string? path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        return OperatingSystem.IsWindows() ? normalized.ToUpperInvariant() : normalized;
    }
}
