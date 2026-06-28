using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Stashes;

[TypeSharp]
public record StashChangesCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IncludeUntracked { get; set; }
}
