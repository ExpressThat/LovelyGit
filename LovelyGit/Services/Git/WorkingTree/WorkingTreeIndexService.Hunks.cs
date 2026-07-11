using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeIndexService
{
    public Task StageHunkAsync(
        string repositoryPath,
        string path,
        string group,
        IReadOnlyList<WorkingTreePatchLine> lines,
        CancellationToken cancellationToken) =>
        ApplyHunkAsync(repositoryPath, path, group, lines, reverse: false, cancellationToken);

    public Task UnstageHunkAsync(
        string repositoryPath,
        string path,
        IReadOnlyList<WorkingTreePatchLine> lines,
        CancellationToken cancellationToken) =>
        ApplyHunkAsync(repositoryPath, path, group: null, lines, reverse: true, cancellationToken);

    private async Task ApplyHunkAsync(
        string repositoryPath,
        string path,
        string? group,
        IReadOnlyList<WorkingTreePatchLine> lines,
        bool reverse,
        CancellationToken cancellationToken)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("A hunk must contain at least one changed line.");
        }

        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var normalizedPath = NormalizeSelectedPaths([path])[0];
        var patch = BuildHunkPatch(normalizedPath, lines);

        var addedIntent = !reverse && string.Equals(group, "Untracked", StringComparison.Ordinal);
        if (addedIntent)
        {
            await _gitCliService
                .CreateCommand(["add", "-N", "--", normalizedPath], repositoryPaths.WorkTreeDirectory)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var arguments = new List<string>
        {
            "apply",
            "--cached",
        };
        if (reverse)
        {
            arguments.Add("--reverse");
        }

        arguments.Add("--unidiff-zero");
        arguments.Add("--whitespace=nowarn");
        arguments.Add("-");
        try
        {
            await _gitCliService
                .CreateCommand(arguments, repositoryPaths.WorkTreeDirectory)
                .WithStandardInputPipe(PipeSource.FromString(patch, Encoding.UTF8))
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            if (addedIntent)
            {
                await _gitCliService
                    .CreateCommand(["reset", "--", normalizedPath], repositoryPaths.WorkTreeDirectory)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }

            throw;
        }
    }
}
