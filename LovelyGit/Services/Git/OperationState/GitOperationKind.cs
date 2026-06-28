using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.OperationState;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<GitOperationKind>))]
public enum GitOperationKind
{
    None,
    Merge,
    Rebase,
    CherryPick,
    Revert,
    Bisect,
}
