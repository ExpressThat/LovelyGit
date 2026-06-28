using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Conflicts;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Conflicts;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GitConflictAction))]
[JsonSerializable(typeof(GitConflictFile))]
[JsonSerializable(typeof(GitConflictTextLine))]
[JsonSerializable(typeof(GitConflictStateResponse))]
[JsonSerializable(typeof(GitConflictFileContentResponse))]
[JsonSerializable(typeof(CommitFileDiffSyntaxSpan))]
[JsonSerializable(typeof(GetConflictStateCommandArguments))]
[JsonSerializable(typeof(GetConflictFileContentCommandArguments))]
[JsonSerializable(typeof(ResolveConflictFileCommandArguments))]
[JsonSerializable(typeof(CompleteConflictOperationCommandArguments))]
[JsonSerializable(typeof(CommandResponse<GitConflictStateResponse>))]
[JsonSerializable(typeof(CommandResponse<GitConflictFileContentResponse>))]
[JsonSerializable(typeof(CommandResponse<EmptyCommandArguments>))]
internal partial class ConflictJsonSerializerContext : JsonSerializerContext
{
}
