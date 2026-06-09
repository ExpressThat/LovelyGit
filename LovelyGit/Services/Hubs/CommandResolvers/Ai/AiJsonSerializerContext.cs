using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Ai.Models;
using ExpressThat.LovelyGit.Services.Hubs.Commands;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Ai;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GenerateCommitMessageCommandArguments))]
[JsonSerializable(typeof(GenerateCommitMessageResponse))]
[JsonSerializable(typeof(AiModelDownloadProgressNotification))]
[JsonSerializable(typeof(GetAiModelLicensesCommandArguments))]
[JsonSerializable(typeof(AiModelLicenseInfo))]
[JsonSerializable(typeof(GetAiModelLicensesResponse))]
[JsonSerializable(typeof(CommandResponse<GenerateCommitMessageResponse>))]
[JsonSerializable(typeof(CommandResponse<GetAiModelLicensesResponse>))]
internal partial class AiJsonSerializerContext : JsonSerializerContext
{
}
