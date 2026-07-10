using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<ConflictResolutionSource>))]
public enum ConflictResolutionSource
{
    Ours,
    Theirs,
    Base,
}
