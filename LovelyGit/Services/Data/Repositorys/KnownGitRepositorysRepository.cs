using AutoDependencyRegistration.Attributes;
using BLite.Core.Query;
using ExpressThat.LovelyGit.Services.Data.Models;
using System.Collections;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys
{
    [RegisterClassAsSingleton]
    public class KnownGitRepositorysRepository
    {
        private AppDbContext _appDbContext { get; set; }

        public KnownGitRepositorysRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<IBLiteQueryable<KnownGitRepository>> GetAllAsync()
        {
            return _appDbContext.KnownGitRepositorys.AsQueryable();
        }

        public async Task<KnownGitRepository> AddAsync(KnownGitRepository repository)
        {
            await _appDbContext.KnownGitRepositorys.InsertAsync(repository);
            await _appDbContext.SaveChangesAsync();

            return repository;
        }

        public async Task RemoveAsync(Guid repositoryId)
        {
            await _appDbContext.KnownGitRepositorys.DeleteAsync(repositoryId);
            await _appDbContext.SaveChangesAsync();
        }

        public async Task<KnownGitRepository> FindByIdAsync(Guid repositoryId)
        {
            return await _appDbContext.KnownGitRepositorys.AsQueryable().FirstAsync(entry => entry.Id == repositoryId);
        }
    }
}
