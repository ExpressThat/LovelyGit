using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

internal abstract class BranchCommandResolverBase<TArguments> : CommandResponder<TArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;

    protected BranchCommandResolverBase(KnownGitRepositorysRepository repositories)
    {
        _repositories = repositories;
    }

    protected async Task<string?> FindRepositoryPathAsync(Guid repositoryId)
    {
        if (repositoryId == Guid.Empty)
        {
            return null;
        }

        var repository = await _repositories.FindByIdAsync(repositoryId).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(repository?.Path) ? null : repository.Path;
    }

    protected static CommandResponseBase Respond(
        NativeCommand<TArguments> command,
        bool isSuccess,
        string? errorMessage = null) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
        };
}
