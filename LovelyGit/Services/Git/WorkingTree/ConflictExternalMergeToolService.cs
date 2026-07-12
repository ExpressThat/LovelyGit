using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class ConflictExternalMergeToolService
{
    private readonly IConflictMergeToolRunner _runner;

    public ConflictExternalMergeToolService(GitCliService git)
        : this(new GitConflictMergeToolRunner(git))
    {
    }

    internal ConflictExternalMergeToolService(IConflictMergeToolRunner runner)
    {
        _runner = runner;
    }

    public async Task OpenAsync(
        string repositoryPath,
        string path,
        CancellationToken cancellationToken)
    {
        path = WorkingTreePath.NormalizeRelative(path);
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var targetPath = WorkingTreePath.Resolve(paths.WorkTreeDirectory, path);
        var indexPath = Path.Combine(paths.WorktreeGitDirectory, "index");
        await EnsureUnmergedAsync(paths.WorkTreeDirectory, paths.GitDirectory, path, cancellationToken)
            .ConfigureAwait(false);

        var snapshot = await ConflictToolSnapshot.CreateAsync(targetPath, indexPath, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var result = await _runner.RunAsync(
                paths.WorkTreeDirectory,
                path,
                cancellationToken).ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(BuildFailureMessage(result.StandardError, result.StandardOutput));
            }

            if (await IsUnmergedAsync(paths.WorkTreeDirectory, paths.GitDirectory, path, cancellationToken)
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(
                    "The external merge tool closed, but Git still reports this file as unmerged.");
            }

            if (await ContainsMarkersAsync(targetPath, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException(
                    "The external merge tool closed, but the result still contains conflict markers.");
            }
        }
        catch
        {
            await snapshot.RestoreAsync(targetPath, indexPath).ConfigureAwait(false);
            throw;
        }
        finally
        {
            snapshot.Dispose();
        }
    }

    private static async Task EnsureUnmergedAsync(
        string workTreeDirectory,
        string gitDirectory,
        string path,
        CancellationToken cancellationToken)
    {
        if (!await IsUnmergedAsync(workTreeDirectory, gitDirectory, path, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("This file no longer has an unresolved conflict.");
        }
    }

    private static async Task<bool> IsUnmergedAsync(
        string workTreeDirectory,
        string gitDirectory,
        string path,
        CancellationToken cancellationToken)
    {
        using var repository = await LovelyGitRepository.OpenAsync(workTreeDirectory, cancellationToken)
            .ConfigureAwait(false);
        var entries = await new GitIndexReader()
            .ReadAsync(gitDirectory, repository.ObjectFormat, cancellationToken)
            .ConfigureAwait(false);
        return entries.Any(entry => entry.Path == path && entry.Stage is >= 1 and <= 3);
    }

    private static string BuildFailureMessage(string standardError, string standardOutput)
    {
        var detail = standardError.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Concat(standardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            .LastOrDefault();
        return string.IsNullOrWhiteSpace(detail)
            ? "The configured external merge tool could not resolve this conflict."
            : $"The external merge tool failed: {detail.Trim()}";
    }

    private static async Task<bool> ContainsMarkersAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return false;
        var text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return ConflictResolutionService.ContainsConflictMarkers(text);
    }
}
