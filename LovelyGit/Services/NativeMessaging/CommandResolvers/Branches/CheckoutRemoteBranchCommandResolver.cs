using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Checkout;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

internal sealed class CheckoutRemoteBranchCommandResolver
    : BranchCommandResolverBase<CheckoutRemoteBranchCommandArguments>
{
    private readonly GitCheckoutCommandService _checkout;

    public CheckoutRemoteBranchCommandResolver(
        GitCheckoutCommandService checkout,
        KnownGitRepositorysRepository repositories) : base(repositories)
    {
        _checkout = checkout;
    }

    protected override JsonTypeInfo<CheckoutRemoteBranchCommandArguments> ArgumentsJsonTypeInfo =>
        BranchesJsonSerializerContext.Default.CheckoutRemoteBranchCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CheckoutRemoteBranch;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CheckoutRemoteBranchCommandArguments> command)
    {
        var arguments = command.Arguments;
        var path = arguments is null ? null : await FindRepositoryPathAsync(arguments.RepositoryId);
        if (arguments is null || path is null)
        {
            return Respond(command, false, "Known repository not found.");
        }

        try
        {
            await _checkout.CheckoutRemoteBranchAsync(
                path,
                arguments.RemoteBranchName,
                arguments.LocalBranchName,
                CancellationToken.None).ConfigureAwait(false);
            return Respond(command, true);
        }
        catch (Exception exception)
        {
            return Respond(command, false, exception.Message);
        }
    }
}
