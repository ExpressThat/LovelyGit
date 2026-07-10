using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.Bisect;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<GitBisectAction>))]
public enum GitBisectAction
{
    Start,
    MarkGood,
    MarkBad,
    Skip,
    Reset,
}

[TypeSharp]
public sealed record GitBisectState
{
    public bool IsActive { get; set; }
    public string? StartingReference { get; set; }
    public string? CurrentCommit { get; set; }
    public string? CurrentSubject { get; set; }
    public string? BadCommit { get; set; }
    public List<string> GoodCommits { get; set; } = [];
    public string? FirstBadCommit { get; set; }
}
