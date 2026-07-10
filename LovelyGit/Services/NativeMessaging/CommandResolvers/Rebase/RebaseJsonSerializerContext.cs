using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.RepositoryOperations;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RebaseCurrentBranchOntoBranchCommandArguments))]
[JsonSerializable(typeof(GetInteractiveRebasePlanCommandArguments))]
[JsonSerializable(typeof(StartInteractiveRebaseCommandArguments))]
[JsonSerializable(typeof(CommandResponse<InteractiveRebasePlanResponse>))]
[JsonSerializable(typeof(CommandResponse<RepositoryOperationCommandResponse>))]
internal partial class RebaseJsonSerializerContext : JsonSerializerContext;
