using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Platform;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class RevealWorkingTreeFileCommandResolver
    : CommandResponder<RevealWorkingTreeFileCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositorysRepository;
    private readonly RepositoryRevealService _revealService;

    protected override JsonTypeInfo<RevealWorkingTreeFileCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.RevealWorkingTreeFileCommandArguments;

    public RevealWorkingTreeFileCommandResolver(
        KnownGitRepositorysRepository repositorysRepository,
        RepositoryRevealService revealService)
    {
        _repositorysRepository = repositorysRepository;
        _revealService = revealService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.RevealWorkingTreeFile;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<RevealWorkingTreeFileCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (string.IsNullOrWhiteSpace(arguments.Path))
        {
            return Failure(command, "Path is required.");
        }

        try
        {
            KnownGitRepository repository =
                await _repositorysRepository.FindByIdAsync(arguments.RepositoryId);
            if (string.IsNullOrWhiteSpace(repository.Path))
            {
                return Failure(command, "Repository path is missing.");
            }

            var path = ResolveWorkingTreePath(repository.Path, arguments.Path);
            await _revealService.RevealPathAsync(path).ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    internal static string ResolveWorkingTreePath(string repositoryPath, string relativePath)
    {
        var root = Path.GetFullPath(repositoryPath);
        var path = Path.GetFullPath(Path.Combine(root, relativePath));
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootPrefix, GetPathComparison()))
        {
            throw new InvalidOperationException("Path must be inside the repository.");
        }

        return path;
    }

    private static StringComparison GetPathComparison() =>
        OperatingSystem.IsLinux()
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

    private static CommandResponseBase Success(
        NativeCommand<RevealWorkingTreeFileCommandArguments> command) =>
        new CommandResponse<EmptyCommandArguments>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = new EmptyCommandArguments(),
        };

    private static CommandResponseBase Failure(
        NativeCommand<RevealWorkingTreeFileCommandArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}
