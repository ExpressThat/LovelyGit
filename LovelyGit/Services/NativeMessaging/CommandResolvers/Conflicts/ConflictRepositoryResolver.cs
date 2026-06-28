using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Conflicts;

internal sealed class ConflictRepositoryResolver
{
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public ConflictRepositoryResolver(KnownGitRepositorysRepository knownRepositories)
    {
        _knownRepositories = knownRepositories;
    }

    public async Task<string?> ResolvePathAsync(Guid repositoryId)
    {
        if (repositoryId == Guid.Empty)
        {
            return null;
        }

        KnownGitRepository foundRepo = await _knownRepositories.FindByIdAsync(repositoryId);
        return string.IsNullOrWhiteSpace(foundRepo?.Path) ? null : foundRepo.Path;
    }

    public static CommandResponseBase Failure(
        string? commandUniqueId,
        NativeMessageType commandType,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = commandUniqueId,
            CommandType = commandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };

    public static CommandResponse<EmptyCommandArguments> EmptySuccess(
        string? commandUniqueId,
        NativeMessageType commandType) =>
        new()
        {
            CommandUniqueId = commandUniqueId,
            CommandType = commandType,
            IsSuccess = true,
            Result = new EmptyCommandArguments(),
        };
}
