using ExpressThat.LovelyGit.Services.TypeGeneration;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Operations;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<GitRepositoryOperationKind>))]
public enum GitRepositoryOperationKind
{
    Merge,
    Rebase,
    CherryPick,
}
