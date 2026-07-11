using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.BranchComparison;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.BranchComparison;

internal sealed class GetBranchComparisonCommandResolver
    : CommandResponder<GetBranchComparisonCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public GetBranchComparisonCommandResolver(KnownGitRepositorysRepository knownRepositories)
    {
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<GetBranchComparisonCommandArguments> ArgumentsJsonTypeInfo =>
        BranchComparisonJsonSerializerContext.Default.GetBranchComparisonCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetBranchComparison;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetBranchComparisonCommandArguments> command)
    {
        if (command.Arguments is not { RepositoryId: var repositoryId } arguments ||
            repositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        var known = await _knownRepositories.FindByIdAsync(repositoryId).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(known?.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var result = arguments is { CurrentCommitHash: { } current, TargetCommitHash: { } target }
                ? await NativeBranchComparisonReader.ReadCommitsAsync(
                    known.Path, current, target, CancellationToken.None).ConfigureAwait(false)
                : await NativeBranchComparisonReader.ReadAsync(
                    known.Path, arguments.TargetBranchName, CancellationToken.None).ConfigureAwait(false);
            return new CommandResponse<BranchComparisonResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = result,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<GetBranchComparisonCommandArguments> command,
        string message) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
