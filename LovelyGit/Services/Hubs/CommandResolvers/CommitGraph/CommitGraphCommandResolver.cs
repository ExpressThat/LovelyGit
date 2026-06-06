using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph
{
    internal class CommitGraphCommandResolver : CommandResponder<CommitGraphCommandArguments>
    {
        private readonly CommitGraphPageService _commitGraphPageService;

        protected override JsonTypeInfo<CommitGraphCommandArguments> ArgumentsJsonTypeInfo =>
            CommitGraphJsonSerializerContext.Default.CommitGraphCommandArguments;

        public CommitGraphCommandResolver(CommitGraphPageService commitGraphPageService)
        {
            _commitGraphPageService = commitGraphPageService;
        }

        public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
        {
            return command.CommandType == CommsHubCommandType.CommitGraph;
        }

        public override async Task<CommandResponseBase> Resolve(CommsHubCommand<CommitGraphCommandArguments> command)
        {
            if (command.Arguments == null)
            {
                return CreateFailureResponse(command, "Invalid commit graph arguments");
            }

            var result = await _commitGraphPageService
                .GetPageAsync(
                    command.Arguments.KnownRepositoryId,
                    command.Arguments.Limit,
                    command.Arguments.Cursor,
                    CancellationToken.None)
                .ConfigureAwait(false);
            if (!result.IsSuccess || result.Response == null)
            {
                return CreateFailureResponse(command, result.ErrorMessage ?? "Failed to load commit graph.");
            }

            return new CommandResponse<CommitGraphResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = result.Response
            };
        }

        private static CommandResponseBase CreateFailureResponse(
            CommsHubCommand<CommitGraphCommandArguments> command,
            string errorMessage)
        {
            return new CommandResponseBase
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = false,
                ErrorMessage = errorMessage,
            };
        }
    }
}
