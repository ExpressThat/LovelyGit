using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.KnownRepository
{
    public class AddKnownGitRepositorysCommandResolver : CommandResponder<EmptyCommandArguments>
    {
        private readonly IFolderPicker _folderPicker;
        private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

        protected override JsonTypeInfo<EmptyCommandArguments> ArgumentsJsonTypeInfo =>
            CommandJsonSerializerContext.Default.EmptyCommandArguments;

        public AddKnownGitRepositorysCommandResolver(
            IFolderPicker folderPicker,
            KnownGitRepositorysRepository knownGitRepositorysRepository)
        {
            _folderPicker = folderPicker;
            _knownGitRepositorysRepository = knownGitRepositorysRepository;
        }

        public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
        {
            return command.CommandType == CommsHubCommandType.AddKnownGitRepositorys;
        }

        public override async Task<CommandResponseBase> Resolve(CommsHubCommand<EmptyCommandArguments> command)
        {
            var selectedFolder = await _folderPicker.PickFolderAsync(CancellationToken.None);
            if (string.IsNullOrWhiteSpace(selectedFolder))
            {
                return new CommandResponse<KnownGitRepository?>
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = true,
                    Result = null,
                };
            }

            var repositoryPath = FindRepositoryRoot(selectedFolder);
            if (repositoryPath == null)
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = "No Git repository found for selected folder.",
                };
            }

            var existingRepository = await _knownGitRepositorysRepository.FindByPathAsync(repositoryPath);
            if (existingRepository != null)
            {
                return CreateSuccessResponse(command, existingRepository);
            }

            var repository = await _knownGitRepositorysRepository.AddAsync(new KnownGitRepository
            {
                Id = Guid.NewGuid(),
                Name = new DirectoryInfo(repositoryPath).Name,
                Path = repositoryPath,
            });

            return CreateSuccessResponse(command, repository);
        }

        private static CommandResponse<KnownGitRepository?> CreateSuccessResponse(
            CommsHubCommand<EmptyCommandArguments> command,
            KnownGitRepository? repository)
        {
            return new CommandResponse<KnownGitRepository?>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = repository,
            };
        }

        private static string? FindRepositoryRoot(string selectedFolder)
        {
            var directory = new DirectoryInfo(Path.GetFullPath(selectedFolder));

            while (directory.Exists)
            {
                var gitPath = Path.Combine(directory.FullName, ".git");
                if (Directory.Exists(gitPath) || File.Exists(gitPath))
                {
                    return Path.TrimEndingDirectorySeparator(directory.FullName);
                }

                if (directory.Parent == null)
                {
                    return null;
                }

                directory = directory.Parent;
            }

            return null;
        }
    }
}
