using ExpressThat.LovelyGit.Services.Git.Submodules;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

[TypeSharp]
public record ManageSubmoduleCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Path { get; set; } = string.Empty;
    public SubmoduleAction Action { get; set; }
}
