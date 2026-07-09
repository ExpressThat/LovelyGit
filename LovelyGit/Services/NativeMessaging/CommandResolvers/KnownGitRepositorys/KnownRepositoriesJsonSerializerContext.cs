using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RemoveKnownGitRepositorysCommandArguments))]
[JsonSerializable(typeof(RevealKnownGitRepositoryCommandArguments))]
[JsonSerializable(typeof(OpenRepositoryTerminalCommandArguments))]
[JsonSerializable(typeof(CloneRepositoryCommandArguments))]
[JsonSerializable(typeof(CancelCloneRepositoryCommandArguments))]
[JsonSerializable(typeof(CloneDestinationResponse))]
[JsonSerializable(typeof(CloneRepositoryProgressNotification))]
[JsonSerializable(typeof(List<KnownGitRepository>))]
[JsonSerializable(typeof(KnownGitRepository))]
[JsonSerializable(typeof(CommandResponse<List<KnownGitRepository>>))]
[JsonSerializable(typeof(CommandResponse<KnownGitRepository?>))]
[JsonSerializable(typeof(CommandResponse<KnownGitRepository>))]
[JsonSerializable(typeof(CommandResponse<CloneDestinationResponse?>))]
[JsonSerializable(typeof(NativeMessageResponse<CloneRepositoryProgressNotification>))]
[JsonSerializable(typeof(CommandResponse<EmptyCommandArguments>))]
internal partial class KnownRepositoriesJsonSerializerContext : JsonSerializerContext
{
}
