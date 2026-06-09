using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Ai;
using ExpressThat.LovelyGit.Services.Ai.Models;
using ExpressThat.LovelyGit.Services.Hubs.Commands;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Ai;

internal sealed class GetAiModelLicensesCommandResolver : CommandResponder<GetAiModelLicensesCommandArguments>
{
    private readonly AiModelDownloadService _modelDownloadService;

    public GetAiModelLicensesCommandResolver(AiModelDownloadService modelDownloadService)
    {
        _modelDownloadService = modelDownloadService;
    }

    protected override JsonTypeInfo<GetAiModelLicensesCommandArguments> ArgumentsJsonTypeInfo =>
        AiJsonSerializerContext.Default.GetAiModelLicensesCommandArguments;

    public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
    {
        return command.CommandType == CommsHubCommandType.GetAiModelLicenses;
    }

    public override async Task<CommandResponseBase> Resolve(CommsHubCommand<GetAiModelLicensesCommandArguments> command)
    {
        try
        {
            var response = await _modelDownloadService.GetLicensesAsync(CancellationToken.None).ConfigureAwait(false);
            return new CommandResponse<GetAiModelLicensesResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = response,
            };
        }
        catch (Exception exception)
        {
            return new CommandResponseBase
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = false,
                ErrorMessage = exception.Message,
            };
        }
    }
}
