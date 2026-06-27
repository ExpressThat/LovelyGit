using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Checkout;

[TypeSharp]
public record CheckoutCommitDetachedCommandArguments
{
    public Guid RepositoryId { get; set; }

    public string CommitHash { get; set; } = string.Empty;
}

[TypeSharp]
public record CheckoutBranchCommandArguments
{
    public Guid RepositoryId { get; set; }

    public string BranchName { get; set; } = string.Empty;
}
