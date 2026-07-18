using BLite.Core.Query;
using ExpressThat.LovelyGit.Services.Data.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys
{
    public class KnownGitRepositorysRepository
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
            using var transaction = _appDbContext.BeginTransaction();
            using var transactionRetention = BLiteTransactionRetention.Track(transaction);
            await _appDbContext.KnownGitRepositorys.InsertAsync(repository, transaction);
            await _appDbContext.SaveChangesAsync(transaction);
            await _orderRepository.AppendRepositoryAsync(repository.Id);

            return repository;
        }

        public async Task RemoveAsync(Guid repositoryId)
        {
            using var transaction = _appDbContext.BeginTransaction();
            using var transactionRetention = BLiteTransactionRetention.Track(transaction);
            await _appDbContext.KnownGitRepositorys.DeleteAsync(repositoryId, transaction);
            await _appDbContext.SaveChangesAsync(transaction);
            await _orderRepository.RemoveRepositoryAsync(repositoryId);
        }

        public async Task<KnownGitRepository> FindByIdAsync(Guid repositoryId)
        {
            return await _appDbContext.KnownGitRepositorys.AsQueryable().FirstAsync(entry => entry.Id == repositoryId);
        }

        public ValueTask<KnownGitRepository?> TryFindByIdAsync(Guid repositoryId) =>
            _appDbContext.KnownGitRepositorys.FindByIdAsync(repositoryId);

        public async Task<KnownGitRepository?> FindByPathAsync(string repositoryPath)
        {
            var normalizedRepositoryPath = NormalizePath(repositoryPath);
            var repositories = await GetAllAsync();

            return repositories.FirstOrDefault(repository =>
                repository.Path != null
                && PathsEqual(NormalizePath(repository.Path), normalizedRepositoryPath));
        }

        private static string NormalizePath(string path)
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }

        private static bool PathsEqual(string left, string right)
        {
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return string.Equals(left, right, comparison);
        }
    }
}
