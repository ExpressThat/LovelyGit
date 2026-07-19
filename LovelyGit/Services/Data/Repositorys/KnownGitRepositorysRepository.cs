using BLite.Core.Query;
using ExpressThat.LovelyGit.Services.Data.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys
{
    public partial class KnownGitRepositorysRepository
    {
        private readonly AppDbContext _appDbContext;
        private readonly KnownGitRepositoryOrderRepository _orderRepository;

        public KnownGitRepositorysRepository(
            AppDbContext appDbContext,
            KnownGitRepositoryOrderRepository orderRepository)
        {
            _appDbContext = appDbContext;
            _orderRepository = orderRepository;
        }

        public async Task<List<KnownGitRepository>> GetAllAsync()
        {
            var repositories = await _appDbContext.KnownGitRepositorys.AsQueryable().ToListAsync();
            return await _orderRepository.ApplyOrderAsync(repositories);
        }

        public async Task<KnownGitRepository> AddAsync(KnownGitRepository repository)
        {
            var pathKey = CreatePathKey(repository.Path);
            var existingPath = await _appDbContext.KnownGitRepositoryPaths.FindByIdAsync(pathKey);
            using var transaction = _appDbContext.BeginTransaction();
            using var transactionRetention = BLiteTransactionRetention.Track(transaction);
            await _appDbContext.KnownGitRepositorys.InsertAsync(repository, transaction);
            var pathEntry = new KnownGitRepositoryPath
            {
                Id = pathKey,
                RepositoryId = repository.Id,
            };
            if (existingPath == null)
            {
                await _appDbContext.KnownGitRepositoryPaths.InsertAsync(pathEntry, transaction);
            }
            else
            {
                await _appDbContext.KnownGitRepositoryPaths.UpdateAsync(pathEntry, transaction);
            }
            await _appDbContext.SaveChangesAsync(transaction);
            await _orderRepository.AppendRepositoryAsync(repository.Id);

            return repository;
        }

        public async Task RemoveAsync(Guid repositoryId)
        {
            var repository = await TryFindByIdAsync(repositoryId);
            var pathKey = repository?.Path is { } path ? CreatePathKey(path) : null;
            var pathEntry = pathKey == null
                ? null
                : await _appDbContext.KnownGitRepositoryPaths.FindByIdAsync(pathKey);
            using var transaction = _appDbContext.BeginTransaction();
            using var transactionRetention = BLiteTransactionRetention.Track(transaction);
            await _appDbContext.KnownGitRepositorys.DeleteAsync(repositoryId, transaction);
            if (pathEntry?.RepositoryId == repositoryId)
            {
                await _appDbContext.KnownGitRepositoryPaths.DeleteAsync(pathKey!, transaction);
            }
            await _appDbContext.SaveChangesAsync(transaction);
            await _orderRepository.RemoveRepositoryAsync(repositoryId);
        }

        public ValueTask<KnownGitRepository> FindByIdAsync(Guid repositoryId)
        {
            var lookup = _appDbContext.KnownGitRepositorys.FindByIdAsync(repositoryId);
            if (!lookup.IsCompletedSuccessfully)
            {
                return CompleteLookupAsync(lookup);
            }

            return lookup.Result is { } repository
                ? new ValueTask<KnownGitRepository>(repository)
                : ValueTask.FromException<KnownGitRepository>(MissingRepositoryException());
        }

        public ValueTask<KnownGitRepository?> TryFindByIdAsync(Guid repositoryId) =>
            _appDbContext.KnownGitRepositorys.FindByIdAsync(repositoryId);

        private static async ValueTask<KnownGitRepository> CompleteLookupAsync(
            ValueTask<KnownGitRepository?> lookup) =>
            await lookup.ConfigureAwait(false) ?? throw MissingRepositoryException();

        private static InvalidOperationException MissingRepositoryException() =>
            new("Sequence contains no elements.");

    }
}
