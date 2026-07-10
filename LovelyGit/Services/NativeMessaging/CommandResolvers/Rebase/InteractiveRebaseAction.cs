using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<InteractiveRebaseAction>))]
public enum InteractiveRebaseAction
{
    Pick,
    Reword,
    Squash,
    Fixup,
    Drop,
}
