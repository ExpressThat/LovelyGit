using ExpressThat.LovelyGit.Services.TypeGeneration;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Git.Submodules;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<SubmoduleState>))]
public enum SubmoduleState
{
    Uninitialized,
    Current,
    DifferentCommit,
    MissingFromHead,
}

[TypeSharp]
public record GitSubmodule
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public string? ExpectedCommit { get; set; }
    public string? CurrentCommit { get; set; }
    public SubmoduleState State { get; set; }
}

internal sealed record GitSubmoduleDefinition(
    string Name,
    string Path,
    string Url,
    string? Branch);
