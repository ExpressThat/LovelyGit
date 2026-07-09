using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

internal sealed class CloneRepositoryCommandResolver : CommandResponder<CloneRepositoryCommandArguments>
{
    private readonly GitCloneService _cloneService;
    private readonly KnownGitRepositorysRepository _knownRepositories;
    private readonly CloneRepositoryProgressPublisher _progressPublisher;

    public CloneRepositoryCommandResolver(
        GitCloneService cloneService,
        KnownGitRepositorysRepository knownRepositories,
        CloneRepositoryProgressPublisher progressPublisher)
    {
        _cloneService = cloneService;
        _knownRepositories = knownRepositories;
        _progressPublisher = progressPublisher;
    }

    protected override JsonTypeInfo<CloneRepositoryCommandArguments> ArgumentsJsonTypeInfo =>
        KnownRepositoriesJsonSerializerContext.Default.CloneRepositoryCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CloneRepository;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CloneRepositoryCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.OperationId == Guid.Empty)
        {
            return Failure(command, "OperationId is required.");
        }

        try
        {
            var repositoryPath = await _cloneService.CloneAsync(
                    arguments.OperationId,
                    arguments.RemoteUrl,
                    arguments.ParentPath,
                    arguments.DirectoryName,
                    arguments.Shallow,
                    arguments.RecurseSubmodules,
                    progress => SendProgress(arguments.OperationId, progress),
                    CancellationToken.None)
                .ConfigureAwait(false);
            var existing = await _knownRepositories.FindByPathAsync(repositoryPath)
                .ConfigureAwait(false);
            var repository = existing ?? await _knownRepositories.AddAsync(new KnownGitRepository
            {
                Id = Guid.NewGuid(),
                Name = new DirectoryInfo(repositoryPath).Name,
                Path = repositoryPath,
            }).ConfigureAwait(false);

            return new CommandResponse<KnownGitRepository>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = repository,
            };
        }
        catch (OperationCanceledException)
        {
            return Failure(command, "Clone canceled.");
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private void SendProgress(Guid operationId, GitCloneProgress progress)
    {
        _progressPublisher.Publish(new CloneRepositoryProgressNotification
        {
            OperationId = operationId,
            Stage = progress.Stage,
            Message = progress.Message,
            Percent = progress.Percent,
            PhasePercent = progress.PhasePercent,
        });
    }

    private static CommandResponseBase Failure(
        NativeCommand<CloneRepositoryCommandArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}
