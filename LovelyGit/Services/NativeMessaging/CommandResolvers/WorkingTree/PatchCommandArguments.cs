using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

[TypeSharp]
public record ApplyPatchCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string PatchPath { get; set; } = string.Empty;
    public bool StageChanges { get; set; }
    public bool Reverse { get; set; }
}
