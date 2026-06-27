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

[TypeSharp]
public record CheckoutRemoteBranchCommandArguments
{
    public Guid RepositoryId { get; set; }

    public string RemoteBranchName { get; set; } = string.Empty;

    public string LocalBranchName { get; set; } = string.Empty;
}

[TypeSharp]
public record CheckoutTagCommandArguments
{
    public Guid RepositoryId { get; set; }

    public string TagName { get; set; } = string.Empty;
}
