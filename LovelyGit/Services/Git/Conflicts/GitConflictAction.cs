using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.Conflicts;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<GitConflictAction>))]
public enum GitConflictAction
{
    UseOurs,
    UseTheirs,
    MarkResolved,
}
