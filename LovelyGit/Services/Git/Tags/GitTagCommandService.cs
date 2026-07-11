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
        bool sign,
        string? message,
        CancellationToken cancellationToken)
    {
        var name = NormalizeTagName(tagName);
        var target = NormalizeCommitHash(commitHash);
        await EnsureCommitExistsAsync(repositoryPath, target, cancellationToken)
            .ConfigureAwait(false);
        if (sign && !isAnnotated)
        {
            throw new ArgumentException("Signed tags must be annotated.", nameof(isAnnotated));
        }

        IReadOnlyList<string> arguments = isAnnotated
            ? ["tag", sign ? "--sign" : "--annotate", "--message", NormalizeMessage(message), "--", name, target]
            : ["tag", "--", name, target];
        await RunAsync(
            repositoryPath,
            "Create tag",
            arguments,
            sign
                ? "Configure user.signingKey and gpg.format, then verify the target commit exists."
                : "Choose a unique valid tag name and verify the target commit exists.",
            cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteTagAsync(
        string repositoryPath,
        string tagName,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            "Delete tag",
            ["tag", "--delete", "--", NormalizeTagName(tagName)],
            "Verify the local tag still exists.",
            cancellationToken);

    public Task PushTagAsync(
        string repositoryPath,
        string remoteName,
        string tagName,
        CancellationToken cancellationToken)
    {
        var remote = NormalizeRemoteName(remoteName);
        var name = NormalizeTagName(tagName);
        return RunAsync(
            repositoryPath,
            "Push tag",
            ["push", remote, $"refs/tags/{name}:refs/tags/{name}"],
            "Check authentication, remote permissions, and whether the remote tag already differs.",
            cancellationToken);
    }

    public Task DeleteRemoteTagAsync(
        string repositoryPath,
        string remoteName,
        string tagName,
        CancellationToken cancellationToken)
    {
        var remote = NormalizeRemoteName(remoteName);
        var name = NormalizeTagName(tagName);
        return RunAsync(
            repositoryPath,
            "Delete remote tag",
            ["push", remote, "--delete", $"refs/tags/{name}"],
            "Check authentication, remote permissions, and whether the remote tag still exists.",
            cancellationToken);
    }

    private async Task RunAsync(
        string repositoryPath,
        string operationName,
        IReadOnlyList<string> arguments,
        string recoveryHint,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        await _gitOperationService.ExecuteRequiredBufferedAsync(
            operationName,
            arguments,
            paths.WorkTreeDirectory,
            recoveryHint,
            cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeTagName(string tagName)
    {
        var normalized = tagName.Trim();
        return GitTagNameValidator.IsValidTagName(normalized)
            ? normalized
            : throw new ArgumentException("Tag name is not valid.", nameof(tagName));
    }

    private static string NormalizeCommitHash(string commitHash)
    {
        var normalized = commitHash.Trim();
        if ((normalized.Length is not 40 and not 64) ||
            normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("Commit hash is not valid.", nameof(commitHash));
        }
        return normalized;
    }

    private static async Task EnsureCommitExistsAsync(
        string repositoryPath,
        string commitHash,
        CancellationToken cancellationToken)
    {
        using var repository = await LovelyGitRepository
            .OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        if (!GitObjectId.TryParse(commitHash, repository.ObjectFormat, out var id))
        {
            throw new ArgumentException("Commit hash does not match this repository.", nameof(commitHash));
        }

        try
        {
            if (await repository.GetCommitAsync(id, cancellationToken).ConfigureAwait(false) is not null)
            {
                return;
            }
        }
        catch (FileNotFoundException)
        {
        }

        throw new ArgumentException("Target commit does not exist.", nameof(commitHash));
    }

    private static string NormalizeRemoteName(string remoteName) =>
        GitRemoteNameValidator.IsValidRemoteName(remoteName)
            ? remoteName.Trim()
            : throw new ArgumentException("Remote name is not valid.", nameof(remoteName));

    private static string NormalizeMessage(string? message) =>
        string.IsNullOrWhiteSpace(message)
            ? throw new ArgumentException("An annotated tag message is required.", nameof(message))
            : message.Trim();
}
