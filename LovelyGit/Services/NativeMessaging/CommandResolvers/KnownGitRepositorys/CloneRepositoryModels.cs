using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

[TypeSharp]
public sealed record CloneDestinationResponse
{
    public string ParentPath { get; init; } = string.Empty;
}

[TypeSharp]
public sealed record CloneRepositoryCommandArguments
{
    public Guid OperationId { get; init; }
    public string RemoteUrl { get; init; } = string.Empty;
    public string ParentPath { get; init; } = string.Empty;
    public string DirectoryName { get; init; } = string.Empty;
    public bool Shallow { get; init; }
    public bool RecurseSubmodules { get; init; }
}

[TypeSharp]
public sealed record CancelCloneRepositoryCommandArguments
{
    public Guid OperationId { get; init; }
}

[TypeSharp]
public sealed record InitializeRepositoryCommandArguments
{
    public string ParentPath { get; init; } = string.Empty;
    public string DirectoryName { get; init; } = string.Empty;
    public string InitialBranchName { get; init; } = string.Empty;
}

[TypeSharp]
public sealed record CloneRepositoryProgressNotification
{
    public Guid OperationId { get; init; }
    public string Stage { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int? Percent { get; init; }
    public int? PhasePercent { get; init; }
}
