using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

internal sealed class RenameBranchCommandResolver
    : BranchCommandResolverBase<RenameBranchCommandArguments>
{
    private readonly GitBranchCommandService _branches;

    public RenameBranchCommandResolver(
        GitBranchCommandService branches,
        KnownGitRepositorysRepository repositories) : base(repositories)
    {
        _branches = branches;
    }

    protected override JsonTypeInfo<RenameBranchCommandArguments> ArgumentsJsonTypeInfo =>
        BranchesJsonSerializerContext.Default.RenameBranchCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.RenameBranch;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<RenameBranchCommandArguments> command)
    {
        var arguments = command.Arguments;
        var path = arguments is null ? null : await FindRepositoryPathAsync(arguments.RepositoryId);
        if (arguments is null || path is null)
        {
            return Respond(command, false, "Known repository not found.");
        }

        try
        {
            await _branches.RenameBranchAsync(
                path,
                arguments.BranchName,
                arguments.NewBranchName,
                CancellationToken.None).ConfigureAwait(false);
            return Respond(command, true);
        }
        catch (Exception exception)
        {
            return Respond(command, false, exception.Message);
        }
    }
}
