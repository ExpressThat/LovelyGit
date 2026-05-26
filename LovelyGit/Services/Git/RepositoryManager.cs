using LibGit2Sharp;

namespace ExpressThat.LovelyGit.Services.Git
{
    public class RepositoryManager
    {
        private Repository? _repository;
        private bool _repositoryOpened = false;


        public RepositoryManager()
        {
    
        }

        public async Task OpenRepository(string path)
        {
            _repository = await Task.Run(() => new Repository(path));
            _repositoryOpened = true;
        }

        public Repository GetRepository()
        {
            if (_repository == null)
            {
                throw new InvalidOperationException("Repository not opened. Call OpenRepository first.");
            }
            return _repository;
        }

        public bool IsRepositoryOpened()
        {
            return _repositoryOpened;
        }
    }
}
