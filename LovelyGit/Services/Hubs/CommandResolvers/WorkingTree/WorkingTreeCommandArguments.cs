using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.WorkingTree;

[TranspilationSource]
public record GetWorkingTreeChangesCommandArguments
{
    public Guid RepositoryId { get; set; }
}

[TranspilationSource]
public record UpdateWorkingTreeIndexCommandArguments
{
    public Guid RepositoryId { get; set; }
    public bool IncludeAll { get; set; }
    public List<string> Paths { get; set; } = new();
}

[TranspilationSource]
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
