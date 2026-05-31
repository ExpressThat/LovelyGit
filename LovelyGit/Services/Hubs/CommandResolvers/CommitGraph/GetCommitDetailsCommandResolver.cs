using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;

public class GetCommitDetailsCommandResolver : CommandResponder<GetCommitDetailsCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    protected override JsonTypeInfo<GetCommitDetailsCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.GetCommitDetailsCommandArguments;

    public GetCommitDetailsCommandResolver(KnownGitRepositorysRepository knownGitRepositorysRepository)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
    }

    public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
    {
        return command.CommandType == CommsHubCommandType.GetCommitDetails;
    }

    public override async Task<CommandResponseBase> Resolve(CommsHubCommand<GetCommitDetailsCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (!GitObjectId.TryParse(command.Arguments.CommitHash, out var commitId))
        {
            return Failure(command, "CommitHash is invalid.");
        }

        KnownGitRepository foundRepo = await _knownGitRepositorysRepository.FindByIdAsync(command.Arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            using var repository = await LovelyGitRepository.OpenAsync(foundRepo.Path, CancellationToken.None)
                .ConfigureAwait(false);
            var commit = await repository.GetCommitAsync(commitId, CancellationToken.None).ConfigureAwait(false);
            GitCommit? firstParent = null;
            if (commit.ParentHashes.Count > 0)
            {
                firstParent = await repository.GetCommitAsync(commit.ParentHashes[0], CancellationToken.None)
                    .ConfigureAwait(false);
            }

            var response = await new CommitDetailsBuilder(repository)
                .BuildAsync(commit, firstParent, CancellationToken.None)
                .ConfigureAwait(false);

            return new CommandResponse<CommitDetailsResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = response
            };
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Failure(
        CommsHubCommand<GetCommitDetailsCommandArguments> command,
        string errorMessage)
    {
        return new CommandResponseBase
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
