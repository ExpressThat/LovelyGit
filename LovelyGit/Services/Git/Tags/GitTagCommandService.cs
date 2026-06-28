using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Tags;

internal sealed class GitTagCommandService
{
    private readonly GitOperationService _gitOperationService;

    public GitTagCommandService(GitOperationService gitOperationService)
    {
        _gitOperationService = gitOperationService;
    }

    public async Task CreateTagAsync(
        string repositoryPath,
        string tagName,
        string commitHash,
        bool isAnnotated,
        string message,
        CancellationToken cancellationToken)
    {
        if (!GitTagNameValidator.IsValidTagName(tagName))
        {
            throw new ArgumentException("Tag name is not valid.", nameof(tagName));
        }

        if (!GitObjectId.TryParse(commitHash, out _))
        {
            throw new ArgumentException("Commit hash is not valid.", nameof(commitHash));
        }

        message = message.Trim();
        if (isAnnotated && string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Annotated tag message is required.", nameof(message));
        }

        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var arguments = isAnnotated
            ? new[] { "tag", "-a", tagName, commitHash, "-m", message }
            : ["tag", "--", tagName, commitHash];

        await _gitOperationService.ExecuteRequiredBufferedAsync(
            "Create local tag",
            arguments,
            repositoryPaths.WorkTreeDirectory,
            "Choose a unique tag name or delete the existing tag first.",
            cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteTagAsync(
        string repositoryPath,
        string tagName,
        CancellationToken cancellationToken)
    {
        if (!GitTagNameValidator.IsValidTagName(tagName))
        {
            throw new ArgumentException("Tag name is not valid.", nameof(tagName));
        }

        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        await _gitOperationService.ExecuteRequiredBufferedAsync(
            "Delete local tag",
            ["tag", "-d", "--", tagName],
            repositoryPaths.WorkTreeDirectory,
            "Refresh tags and confirm the tag still exists locally.",
            cancellationToken).ConfigureAwait(false);
    }

    public async Task PushTagAsync(
        string repositoryPath,
        string remoteName,
        string tagName,
        CancellationToken cancellationToken)
    {
        if (!GitRemoteNameValidator.IsValidRemoteName(remoteName))
        {
            throw new ArgumentException("Remote name is not valid.", nameof(remoteName));
        }

        if (!GitTagNameValidator.IsValidTagName(tagName))
        {
            throw new ArgumentException("Tag name is not valid.", nameof(tagName));
        }

        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        await _gitOperationService.ExecuteRequiredBufferedAsync(
            "Push tag",
            ["push", remoteName, $"refs/tags/{tagName}:refs/tags/{tagName}"],
            repositoryPaths.WorkTreeDirectory,
            "Confirm the remote exists and accepts tag updates.",
            cancellationToken).ConfigureAwait(false);
    }
}
