using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.RepositoryOperations;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RebaseCurrentBranchOntoBranchCommandArguments))]
[JsonSerializable(typeof(CommandResponse<BranchIntegrationCommandResponse>))]
internal partial class RebaseJsonSerializerContext : JsonSerializerContext;
