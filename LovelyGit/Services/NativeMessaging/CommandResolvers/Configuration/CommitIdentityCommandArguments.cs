using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Configuration;

[TypeSharp]
public sealed record GetCommitIdentityCommandArguments
{
    public Guid RepositoryId { get; set; }
}

[TypeSharp]
public sealed record ManageCommitIdentityCommandArguments
{
    public Guid RepositoryId { get; set; }
    public bool ClearRepositoryOverride { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}
