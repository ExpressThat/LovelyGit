using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CherryPick;

[JsonSerializable(typeof(CherryPickCommitCommandArguments))]
[JsonSerializable(typeof(CommandResponse<EmptyCommandArguments>))]
internal partial class CherryPickJsonSerializerContext : JsonSerializerContext
{
}
