using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Operations;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.RepositoryOperations;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GitRepositoryOperationKind))]
[JsonSerializable(typeof(BranchIntegrationCommandResponse))]
[JsonSerializable(typeof(GetRepositoryOperationStateCommandArguments))]
[JsonSerializable(typeof(RepositoryOperationCommandArguments))]
[JsonSerializable(typeof(RepositoryOperationStateResponse))]
[JsonSerializable(typeof(CommandResponse<BranchIntegrationCommandResponse>))]
[JsonSerializable(typeof(CommandResponse<RepositoryOperationStateResponse>))]
internal partial class RepositoryOperationsJsonSerializerContext : JsonSerializerContext;
