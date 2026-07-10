using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<GitPushMode>))]
public enum GitPushMode
{
    Normal,
    ForceWithLease,
}
