using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Reflog;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal sealed class GetReflogCommandResolver : CommandResponder<GetReflogCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;

    public GetReflogCommandResolver(KnownGitRepositorysRepository repositories)
    {
        _repositories = repositories;
    }

    protected override JsonTypeInfo<GetReflogCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.GetReflogCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetReflog;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetReflogCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.KnownRepositoryId == Guid.Empty)
        {
            return Failure(command, "KnownRepositoryId is required.");
        }

        var repository = await _repositories.FindByIdAsync(arguments.KnownRepositoryId);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var response = await GitReflogReader.ReadAsync(
                repository.Path,
                arguments.BranchName,
                arguments.Limit,
                CancellationToken.None).ConfigureAwait(false);
            return new CommandResponse<GitReflogResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = response,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<GetReflogCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
