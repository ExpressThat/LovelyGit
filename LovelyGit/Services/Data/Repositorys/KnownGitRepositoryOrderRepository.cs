using ExpressThat.LovelyGit.Services.Data.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys
{
    public class KnownGitRepositoryOrderRepository
    {
        private const string DefaultOrderId = "default";

        private readonly AppDbContext _appDbContext;

        public KnownGitRepositoryOrderRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<List<Guid>> GetOrderAsync()
        {
            var order = await _appDbContext.KnownGitRepositoryOrders.FindByIdAsync(DefaultOrderId);
            return order?.RepositoryIds ?? [];
        }

        public async Task SaveOrderAsync(IEnumerable<Guid> repositoryIds)
        {
            var model = new KnownGitRepositoryOrder
            {
                Id = DefaultOrderId,
                RepositoryIds = Deduplicate(repositoryIds),
            };

            var existing = await _appDbContext.KnownGitRepositoryOrders.FindByIdAsync(DefaultOrderId);
            if (existing == null)
            {
                await _appDbContext.KnownGitRepositoryOrders.InsertAsync(model);
            }
            else
            {
                await _appDbContext.KnownGitRepositoryOrders.UpdateAsync(model);
            }

            await _appDbContext.SaveChangesAsync();
        }

        public async Task<List<KnownGitRepository>> ApplyOrderAsync(IReadOnlyList<KnownGitRepository> repositories)
        {
            var order = await GetOrderAsync();
            if (order.Count == 0)
            {
                return repositories.ToList();
            }

            var repositoriesById = repositories.ToDictionary(repository => repository.Id);
            var orderedRepositories = new List<KnownGitRepository>();
            var orderedIds = new HashSet<Guid>();

            foreach (var repositoryId in order)
            {
                if (repositoriesById.TryGetValue(repositoryId, out var repository) && orderedIds.Add(repositoryId))
                {
                    orderedRepositories.Add(repository);
                }
            }

            foreach (var repository in repositories)
            {
                if (orderedIds.Add(repository.Id))
                {
                    orderedRepositories.Add(repository);
                }
            }

            return orderedRepositories;
        }

        public async Task RemoveRepositoryAsync(Guid repositoryId)
        {
            var order = await GetOrderAsync();
            if (!order.Remove(repositoryId))
            {
                return;
            }

            await SaveOrderAsync(order);
        }

        public async Task AppendRepositoryAsync(Guid repositoryId)
        {
            var order = await GetOrderAsync();
            if (order.Contains(repositoryId))
            {
                return;
            }

            order.Add(repositoryId);
            await SaveOrderAsync(order);
        }

        private static List<Guid> Deduplicate(IEnumerable<Guid> repositoryIds)
        {
            var seenRepositoryIds = new HashSet<Guid>();
            var deduplicatedRepositoryIds = new List<Guid>();

            foreach (var repositoryId in repositoryIds)
            {
                if (seenRepositoryIds.Add(repositoryId))
                {
                    deduplicatedRepositoryIds.Add(repositoryId);
                }
            }

            return deduplicatedRepositoryIds;
        }
    }
}
