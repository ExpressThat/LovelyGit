using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class PullRepositoryCommandResolver : GitRemoteCommandResolver
{
    private readonly GitRemoteCommandService _gitRemoteCommandService;

    public PullRepositoryCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitRemoteCommandService gitRemoteCommandService) : base(knownGitRepositorysRepository)
    {
        _gitRemoteCommandService = gitRemoteCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.PullRepository;
    }

    protected override Task RunAsync(
        string repositoryPath,
        GitRemoteCommandArguments arguments,
        CancellationToken cancellationToken)
    {
        return _gitRemoteCommandService.PullAsync(
            repositoryPath,
            arguments.PullMode,
            cancellationToken);
    }
}
