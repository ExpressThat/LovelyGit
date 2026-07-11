using ExpressThat.LovelyGit.Services.Git.SparseCheckout;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.SparseCheckout;

[TypeSharp]
public sealed record GetSparseCheckoutStateCommandArguments
{
    public Guid RepositoryId { get; init; }
}

[TypeSharp]
public sealed record ManageSparseCheckoutCommandArguments
{
    public Guid RepositoryId { get; init; }
    public SparseCheckoutAction Action { get; init; }
    public bool ConeMode { get; init; }
    public List<string> Patterns { get; init; } = [];
}
