using System.Buffers;
using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeIndexService
{
    private static readonly Encoding PathspecEncoding = Encoding.UTF8;
    private static readonly byte[] Nul = [0];

    private readonly GitCliService _gitCliService;

    public WorkingTreeIndexService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task StageAsync(
        string repositoryPath,
        IReadOnlyList<string> paths,
        bool includeAll,
        CancellationToken cancellationToken)
    {
        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        if (includeAll)
        {
            await _gitCliService
                .CreateCommand(["add", "-A"], repositoryPaths.WorkTreeDirectory)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var normalizedPaths = NormalizeSelectedPaths(paths);
        await _gitCliService
            .CreateCommand(
                ["add", "-A", "--pathspec-from-file=-", "--pathspec-file-nul"],
                repositoryPaths.WorkTreeDirectory)
            .WithStandardInputPipe(PipeSource.Create((stream, token) => WritePathspecsAsync(stream, normalizedPaths, token)))
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UnstageAsync(
        string repositoryPath,
        IReadOnlyList<string> paths,
        bool includeAll,
        CancellationToken cancellationToken)
    {
        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        if (includeAll)
        {
            await _gitCliService
                .CreateCommand(["reset", "-q", "HEAD", "--", "."], repositoryPaths.WorkTreeDirectory)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var normalizedPaths = NormalizeSelectedPaths(paths);
        await _gitCliService
            .CreateCommand(
                ["reset", "-q", "--pathspec-from-file=-", "--pathspec-file-nul", "HEAD"],
                repositoryPaths.WorkTreeDirectory)
            .WithStandardInputPipe(PipeSource.Create((stream, token) => WritePathspecsAsync(stream, normalizedPaths, token)))
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DiscardChangesAsync(
        string repositoryPath,
        IReadOnlyList<WorkingTreeChangedFile> files,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            throw new InvalidOperationException("Select at least one file to discard.");
        }

        var trackedPaths = files
            .Where(file => file.Group == WorkingTreeChangeGroup.Unstaged)
            .Select(file => file.Path)
            .ToArray();
        var untrackedPaths = files
            .Where(file => file.Group == WorkingTreeChangeGroup.Untracked)
            .Select(file => file.Path)
            .ToArray();

        if (trackedPaths.Length + untrackedPaths.Length != files.Count)
        {
            throw new InvalidOperationException("Only unstaged and untracked files can be discarded.");
        }

        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        if (trackedPaths.Length > 0)
        {
            await _gitCliService
                .CreateCommand(
                    ["restore", "--worktree", "--pathspec-from-file=-", "--pathspec-file-nul"],
                    repositoryPaths.WorkTreeDirectory)
                .WithStandardInputPipe(PipeSource.Create((stream, token) =>
                    WritePathspecsAsync(stream, NormalizeSelectedPaths(trackedPaths), token)))
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        if (untrackedPaths.Length > 0)
        {
            var cleanArguments = new List<string>(untrackedPaths.Length + 3)
            {
                "clean",
                "-f",
                "--",
            };
            cleanArguments.AddRange(NormalizeSelectedPaths(untrackedPaths));

            await _gitCliService
                .CreateCommand(cleanArguments, repositoryPaths.WorkTreeDirectory)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task StageLineAsync(
        string repositoryPath,
        string path,
        string group,
        string changeType,
        int? oldLineNumber,
        int? newLineNumber,
        string oldText,
        string newText,
        CancellationToken cancellationToken)
    {
        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var normalizedPath = NormalizeSelectedPaths([path])[0];

        if (string.Equals(group, "Untracked", StringComparison.Ordinal))
        {
            await _gitCliService
                .CreateCommand(["add", "-N", "--", normalizedPath], repositoryPaths.WorkTreeDirectory)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var patch = BuildSingleLinePatch(
            normalizedPath,
            changeType,
            oldLineNumber,
            newLineNumber,
            oldText,
            newText);

        await _gitCliService
            .CreateCommand(
                ["apply", "--cached", "--unidiff-zero", "--whitespace=nowarn", "-"],
                repositoryPaths.WorkTreeDirectory)
            .WithStandardInputPipe(PipeSource.FromString(patch, Encoding.UTF8))
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UnstageLineAsync(
        string repositoryPath,
        string path,
        string changeType,
        int? oldLineNumber,
        int? newLineNumber,
        string oldText,
        string newText,
        CancellationToken cancellationToken)
    {
        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var normalizedPath = NormalizeSelectedPaths([path])[0];
        var patch = BuildSingleLinePatch(
            normalizedPath,
            changeType,
            oldLineNumber,
            newLineNumber,
            oldText,
            newText);

        await _gitCliService
            .CreateCommand(
                ["apply", "--cached", "--reverse", "--unidiff-zero", "--whitespace=nowarn", "-"],
                repositoryPaths.WorkTreeDirectory)
            .WithStandardInputPipe(PipeSource.FromString(patch, Encoding.UTF8))
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task CommitStagedChangesAsync(
        string repositoryPath,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length == 0)
        {
            throw new InvalidOperationException("Commit title is required.");
        }

        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        var trimmedBody = body.Trim();
        var arguments = trimmedBody.Length == 0
            ? new[] { "commit", "-m", trimmedTitle }
            : new[] { "commit", "-m", trimmedTitle, "-m", trimmedBody };
        var result = await _gitCliService
            .ExecuteBufferedAsync(
                arguments,
                repositoryPaths.WorkTreeDirectory,
                validateExitCode: false,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode == 0)
        {
            return;
        }

        var message = FirstNonEmptyLine(result.StandardError)
            ?? FirstNonEmptyLine(result.StandardOutput)
            ?? "Git could not create the commit.";
        throw new InvalidOperationException(message);
    }

}
