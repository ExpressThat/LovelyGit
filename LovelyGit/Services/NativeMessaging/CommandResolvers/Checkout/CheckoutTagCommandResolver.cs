using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Checkout;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Checkout;

internal sealed class CheckoutTagCommandResolver
    : CommandResponder<CheckoutTagCommandArguments>
{
    private readonly GitCheckoutCommandService _checkout;
    private readonly KnownGitRepositorysRepository _repositories;

    public CheckoutTagCommandResolver(
        GitCheckoutCommandService checkout,
        KnownGitRepositorysRepository repositories)
    {
        _checkout = checkout;
        _repositories = repositories;
    }

    protected override JsonTypeInfo<CheckoutTagCommandArguments> ArgumentsJsonTypeInfo =>
        CheckoutJsonSerializerContext.Default.CheckoutTagCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CheckoutTag;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CheckoutTagCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Respond(command, false, "RepositoryId is required.");
        }

        var repository = await _repositories.FindByIdAsync(arguments.RepositoryId)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(repository?.Path))
        {
            return Respond(command, false, "Known repository not found.");
        }

        try
        {
            await _checkout.CheckoutTagAsync(
                repository.Path,
                arguments.TagName,
                CancellationToken.None).ConfigureAwait(false);
            return Respond(command, true, null);
        }
        catch (Exception exception)
        {
            return Respond(command, false, exception.Message);
        }
    }

    private static CommandResponseBase Respond(
        NativeCommand<CheckoutTagCommandArguments> command,
        bool success,
        string? error) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = success,
            ErrorMessage = error,
        };
}
