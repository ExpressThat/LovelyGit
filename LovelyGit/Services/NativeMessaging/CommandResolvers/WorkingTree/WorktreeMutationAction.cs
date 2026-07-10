using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<WorktreeMutationAction>))]
public enum WorktreeMutationAction
{
    Open,
    Reveal,
    Terminal,
    Lock,
    Unlock,
    Remove,
}
