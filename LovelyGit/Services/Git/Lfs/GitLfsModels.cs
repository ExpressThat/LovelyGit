using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.Lfs;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<GitLfsAction>))]
public enum GitLfsAction
{
    Install,
    Track,
    Untrack,
    Fetch,
    Pull,
    Prune,
}
