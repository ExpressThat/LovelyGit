using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

internal sealed class DeleteBranchCommandResolver
    : BranchCommandResolverBase<DeleteBranchCommandArguments>
{
    private readonly GitBranchCommandService _branches;

    public DeleteBranchCommandResolver(
        GitBranchCommandService branches,
        KnownGitRepositorysRepository repositories) : base(repositories)
    {
        _branches = branches;
    }

    protected override JsonTypeInfo<DeleteBranchCommandArguments> ArgumentsJsonTypeInfo =>
        BranchesJsonSerializerContext.Default.DeleteBranchCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.DeleteBranch;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<DeleteBranchCommandArguments> command)
    {
        var arguments = command.Arguments;
        var path = arguments is null ? null : await FindRepositoryPathAsync(arguments.RepositoryId);
        if (arguments is null || path is null)
        {
            return Respond(command, false, "Known repository not found.");
        }

        try
        {
            await _branches.DeleteBranchAsync(
                path,
                arguments.BranchName,
                arguments.Force,
                CancellationToken.None).ConfigureAwait(false);
            return Respond(command, true);
        }
        catch (Exception exception)
        {
            return Respond(command, false, exception.Message);
        }
    }
}
