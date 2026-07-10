using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed partial class GitRemoteCommandService
{
    public async Task AddAsync(
        string repositoryPath,
        string name,
        string url,
        string? pushUrl,
        CancellationToken cancellationToken)
    {
        var remote = NormalizeName(name);
        var fetch = NormalizeUrl(url, nameof(url));
        var push = NormalizeOptionalUrl(pushUrl, nameof(pushUrl));
        await RunRemoteCommandAsync(
            repositoryPath,
            ["remote", "add", remote, fetch],
            cancellationToken).ConfigureAwait(false);
        try
        {
            if (push != null && !push.Equals(fetch, StringComparison.Ordinal))
            {
                await SetPushUrlAsync(repositoryPath, remote, push, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            await TryRemoveAsync(repositoryPath, remote, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task UpdateAsync(
        string repositoryPath,
        string name,
        string newName,
        string url,
        string? pushUrl,
        CancellationToken cancellationToken)
    {
        var current = NormalizeName(name);
        var next = NormalizeName(newName);
        var fetch = NormalizeUrl(url, nameof(url));
        var push = NormalizeOptionalUrl(pushUrl, nameof(pushUrl));
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var configured = await GitRemoteConfigReader
            .ReadRemotesAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var existing = configured.Find(remote => remote.Name.Equals(current, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Remote '{current}' was not found.");
        if (!current.Equals(next, StringComparison.Ordinal))
        {
            await RunRemoteCommandAsync(
                repositoryPath,
                ["remote", "rename", current, next],
                cancellationToken).ConfigureAwait(false);
        }

        if (!existing.Url.Equals(fetch, StringComparison.Ordinal))
        {
            await RunRemoteCommandAsync(
                repositoryPath,
                ["remote", "set-url", next, fetch],
                cancellationToken).ConfigureAwait(false);
        }

        var desiredPush = push == null || push.Equals(fetch, StringComparison.Ordinal) ? null : push;
        if (!string.Equals(existing.PushUrl, desiredPush, StringComparison.Ordinal))
        {
            if (desiredPush == null)
            {
                await ClearPushUrlAsync(repositoryPath, next, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await SetPushUrlAsync(repositoryPath, next, desiredPush, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public Task RemoveAsync(
        string repositoryPath,
        string name,
        CancellationToken cancellationToken) =>
        RunRemoteCommandAsync(
            repositoryPath,
            ["remote", "remove", NormalizeName(name)],
            cancellationToken);

    private Task SetPushUrlAsync(
        string repositoryPath,
        string name,
        string pushUrl,
        CancellationToken cancellationToken) =>
        RunRemoteCommandAsync(
            repositoryPath,
            ["remote", "set-url", "--push", name, pushUrl],
            cancellationToken);

    private async Task ClearPushUrlAsync(
        string repositoryPath,
        string name,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var result = await _gitCliService.ExecuteBufferedAsync(
            ["config", "--unset-all", $"remote.{name}.pushurl"],
            paths.WorkTreeDirectory,
            validateExitCode: false,
            cancellationToken).ConfigureAwait(false);
        if (result.ExitCode is not (0 or 5))
        {
            throw new InvalidOperationException("Git could not clear the remote push URL.");
        }
    }

    private async Task TryRemoveAsync(
        string repositoryPath,
        string name,
        CancellationToken cancellationToken)
    {
        try
        {
            await RemoveAsync(repositoryPath, name, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Preserve the original add failure.
        }
    }

    private static string NormalizeName(string name) =>
        GitRemoteNameValidator.IsValidRemoteName(name)
            ? name.Trim()
            : throw new ArgumentException("Remote name is not valid.", nameof(name));

    private static string NormalizeUrl(string url, string parameterName)
    {
        var normalized = url.Trim();
        if (normalized.Length == 0 || normalized[0] == '-' || normalized.IndexOfAny(['\r', '\n', '\0']) >= 0)
        {
            throw new ArgumentException("Remote URL is not valid.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalUrl(string? url, string parameterName) =>
        string.IsNullOrWhiteSpace(url) ? null : NormalizeUrl(url, parameterName);
}
