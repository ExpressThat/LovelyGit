using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.RepositoryOperations;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Merge;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MergeBranchIntoCurrentCommandArguments))]
[JsonSerializable(typeof(CommandResponse<BranchIntegrationCommandResponse>))]
internal partial class MergeJsonSerializerContext : JsonSerializerContext;
