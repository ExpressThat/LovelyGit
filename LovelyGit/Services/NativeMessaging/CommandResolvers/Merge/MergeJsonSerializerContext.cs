using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Merge;

[JsonSerializable(typeof(MergeBranchIntoCurrentCommandArguments))]
[JsonSerializable(typeof(CommandResponse<EmptyCommandArguments>))]
internal partial class MergeJsonSerializerContext : JsonSerializerContext
{
}
