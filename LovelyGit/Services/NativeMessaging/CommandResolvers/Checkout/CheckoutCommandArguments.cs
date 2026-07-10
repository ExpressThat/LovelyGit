using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Checkout;

[TypeSharp]
public sealed record CheckoutTagCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string TagName { get; init; } = string.Empty;
}
