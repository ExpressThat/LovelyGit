using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Reset;

[JsonSerializable(typeof(GitResetMode))]
[JsonSerializable(typeof(ResetCurrentBranchToCommitCommandArguments))]
[JsonSerializable(typeof(CommandResponse<EmptyCommandArguments>))]
internal partial class ResetJsonSerializerContext : JsonSerializerContext
{
}
