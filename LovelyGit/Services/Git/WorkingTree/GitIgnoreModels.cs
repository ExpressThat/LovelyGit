using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<GitIgnoreTarget>))]
public enum GitIgnoreTarget
{
    Shared,
    Local,
}

[TypeSharp]
public sealed record GitIgnoreResult
{
    public bool Added { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public GitIgnoreTarget Target { get; set; }
    public WorkingTreeChangesResponse? TargetChanges { get; set; }
}
