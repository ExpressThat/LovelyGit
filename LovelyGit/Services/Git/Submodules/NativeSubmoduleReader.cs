using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.Submodules;

internal sealed class NativeSubmoduleReader
{
    public async Task<List<GitSubmodule>> ReadAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var definitions = await GitModulesReader
            .ReadAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        if (definitions.Count == 0) return [];

        using var repository = await LovelyGitRepository
            .OpenObjectDatabaseAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var headId = await repository.ResolveHeadAsync(cancellationToken).ConfigureAwait(false);
        GitCommit? head = headId.HasValue
            ? await repository.GetCommitAsync(headId.Value, cancellationToken)
                .ConfigureAwait(false)
            : null;
        var results = new List<GitSubmodule>(definitions.Count);
        foreach (var definition in definitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var submodulePath = ResolveSubmodulePath(repositoryPath, definition.Path);
            var gitlink = head?.TreeHash == null
                ? null
                : await repository
                    .TryGetTreeFileAsync(head.TreeHash.Value, definition.Path, cancellationToken)
                    .ConfigureAwait(false);
            var expected = gitlink?.Mode == "160000" ? gitlink.ObjectId.ToString() : null;
            var current = submodulePath == null
                ? null
                : await TryReadCurrentCommitAsync(submodulePath, cancellationToken)
                    .ConfigureAwait(false);
            results.Add(new GitSubmodule
            {
                Name = definition.Name,
                Path = definition.Path,
                Url = definition.Url,
                Branch = definition.Branch,
                ExpectedCommit = expected,
                CurrentCommit = current,
                State = GetState(expected, current),
            });
        }

        return results;
    }

    private static async Task<string?> TryReadCurrentCommitAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(path) ||
            (!Directory.Exists(Path.Combine(path, ".git")) && !File.Exists(Path.Combine(path, ".git"))))
        {
            return null;
        }

        try
        {
            var paths = await GitRepositoryDiscovery
                .ResolveRepositoryPathsAsync(path, cancellationToken)
                .ConfigureAwait(false);
            var objectFormat = await GitRepositoryDiscovery
                .ReadObjectFormatAsync(paths.GitDirectory, cancellationToken)
                .ConfigureAwait(false);
            return (await GitHeadReader.ResolveAsync(
                    paths.WorktreeGitDirectory,
                    paths.GitDirectory,
                    objectFormat,
                    cancellationToken)
                .ConfigureAwait(false))?.ToString();
        }
        catch (Exception exception) when (exception is
            InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string? ResolveSubmodulePath(string repositoryPath, string relativePath)
    {
        if (Path.IsPathRooted(relativePath)) return null;
        try
        {
            var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryPath)) +
                Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
            return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? candidate
                : null;
        }
        catch (Exception exception) when (exception is
            ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static SubmoduleState GetState(string? expected, string? current)
    {
        if (expected == null) return SubmoduleState.MissingFromHead;
        if (current == null) return SubmoduleState.Uninitialized;
        return string.Equals(expected, current, StringComparison.Ordinal)
            ? SubmoduleState.Current
            : SubmoduleState.DifferentCommit;
    }
}
