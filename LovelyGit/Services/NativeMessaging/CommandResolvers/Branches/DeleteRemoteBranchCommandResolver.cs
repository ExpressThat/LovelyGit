using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

internal sealed class DeleteRemoteBranchCommandResolver
    : BranchCommandResolverBase<DeleteRemoteBranchCommandArguments>
{
    private readonly GitBranchCommandService _branches;

    public DeleteRemoteBranchCommandResolver(
        GitBranchCommandService branches,
        KnownGitRepositorysRepository repositories) : base(repositories)
    {
        _branches = branches;
    }

    protected override JsonTypeInfo<DeleteRemoteBranchCommandArguments> ArgumentsJsonTypeInfo =>
        BranchesJsonSerializerContext.Default.DeleteRemoteBranchCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.DeleteRemoteBranch;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<DeleteRemoteBranchCommandArguments> command)
    {
        var arguments = command.Arguments;
        var path = arguments is null ? null : await FindRepositoryPathAsync(arguments.RepositoryId);
        if (arguments is null || path is null)
        {
            return Respond(command, false, "Known repository not found.");
        }

        try
        {
            await _branches.DeleteRemoteBranchAsync(
                path,
                arguments.RemoteBranchName,
                CancellationToken.None).ConfigureAwait(false);
            return Respond(command, true);
        }
        catch (Exception exception)
        {
            return Respond(command, false, exception.Message);
        }
    }
}
