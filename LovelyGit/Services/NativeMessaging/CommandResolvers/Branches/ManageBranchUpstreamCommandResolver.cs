using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

internal sealed class ManageBranchUpstreamCommandResolver
    : BranchCommandResolverBase<ManageBranchUpstreamCommandArguments>
{
    private readonly GitBranchCommandService _commands;

    protected override JsonTypeInfo<ManageBranchUpstreamCommandArguments> ArgumentsJsonTypeInfo =>
        BranchesJsonSerializerContext.Default.ManageBranchUpstreamCommandArguments;

    public ManageBranchUpstreamCommandResolver(
        KnownGitRepositorysRepository repositories,
        GitBranchCommandService commands)
        : base(repositories)
    {
        _commands = commands;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ManageBranchUpstream;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<ManageBranchUpstreamCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || string.IsNullOrWhiteSpace(arguments.BranchName))
        {
            return Respond(command, false, "Branch name is required.");
        }

        var path = await FindRepositoryPathAsync(arguments.RepositoryId).ConfigureAwait(false);
        if (path == null)
        {
            return Respond(command, false, "Known repository not found.");
        }

        try
        {
            if (string.IsNullOrWhiteSpace(arguments.UpstreamName))
            {
                await _commands.UnsetBranchUpstreamAsync(
                    path, arguments.BranchName, CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await _commands.SetBranchUpstreamAsync(
                    path,
                    arguments.BranchName,
                    arguments.UpstreamName,
                    CancellationToken.None).ConfigureAwait(false);
            }

            return Respond(command, true);
        }
        catch (Exception exception)
        {
            return Respond(command, false, exception.Message);
        }
    }
}
