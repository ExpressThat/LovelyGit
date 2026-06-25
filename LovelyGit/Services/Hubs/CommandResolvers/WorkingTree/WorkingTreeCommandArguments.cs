using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

[TypeSharp]
public record GetWorkingTreeChangesCommandArguments
{
    public Guid RepositoryId { get; set; }
}

[TypeSharp]
public record UpdateWorkingTreeIndexCommandArguments
{
    public Guid RepositoryId { get; set; }
    public bool IncludeAll { get; set; }
    public List<string> Paths { get; set; } = new();
}

[TypeSharp]
public record StageWorkingTreeLineCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public int? OldLineNumber { get; set; }
    public int? NewLineNumber { get; set; }
    public string OldText { get; set; } = string.Empty;
    public string NewText { get; set; } = string.Empty;
}

[TypeSharp]
public record CommitStagedChangesCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

[TypeSharp]
public record GitRemoteCommandArguments
{
    public Guid RepositoryId { get; set; }
}
