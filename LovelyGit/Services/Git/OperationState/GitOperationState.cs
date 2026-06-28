using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.OperationState;

[TypeSharp]
public sealed record GitOperationState
{
    public GitOperationKind Kind { get; init; }
    public string Label { get; init; } = "Ready";
    public string Description { get; init; } = "No Git operation is in progress.";
    public bool IsInProgress => Kind != GitOperationKind.None;
}
