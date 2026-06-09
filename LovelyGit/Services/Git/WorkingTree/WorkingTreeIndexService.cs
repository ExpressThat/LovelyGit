using System.Buffers;
using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class WorkingTreeIndexService
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

    private static List<string> NormalizeSelectedPaths(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            throw new InvalidOperationException("At least one file path is required.");
        }

        var normalized = new List<string>(paths.Count);
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            if (Path.IsPathRooted(path) || ContainsParentDirectorySegment(path))
            {
                throw new InvalidOperationException("Working tree paths must be repository-relative.");
            }

            var normalizedPath = path.Replace('\\', '/');
            if (!normalized.Contains(normalizedPath, StringComparer.Ordinal))
            {
                normalized.Add(normalizedPath);
            }
        }

        if (normalized.Count == 0)
        {
            throw new InvalidOperationException("At least one file path is required.");
        }

        return normalized;
    }

    private static async Task WritePathspecsAsync(
        Stream stream,
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken)
    {
        foreach (var path in paths)
        {
            var maxByteCount = PathspecEncoding.GetMaxByteCount(path.Length);
            var buffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
            try
            {
                var byteCount = PathspecEncoding.GetBytes(path, buffer);
                await stream.WriteAsync(buffer.AsMemory(0, byteCount), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            await stream.WriteAsync(Nul, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool ContainsParentDirectorySegment(ReadOnlySpan<char> path)
    {
        while (!path.IsEmpty)
        {
            var slashIndex = path.IndexOfAny('/', '\\');
            var segment = slashIndex < 0 ? path : path[..slashIndex];
            if (segment.SequenceEqual(".."))
            {
                return true;
            }

            if (slashIndex < 0)
            {
                return false;
            }

            path = path[(slashIndex + 1)..];
        }

        return false;
    }

    private static string BuildSingleLinePatch(
        string path,
        string changeType,
        int? oldLineNumber,
        int? newLineNumber,
        string oldText,
        string newText)
    {
        return changeType switch
        {
            "Inserted" => BuildInsertedLinePatch(path, oldLineNumber, newLineNumber, newText),
            "Deleted" => BuildDeletedLinePatch(path, oldLineNumber, newLineNumber, oldText),
            "Modified" => BuildModifiedLinePatch(path, oldLineNumber, newLineNumber, oldText, newText),
            _ => throw new InvalidOperationException("Only changed lines can be staged."),
        };
    }

    private static string BuildInsertedLinePatch(
        string path,
        int? oldLineNumber,
        int? newLineNumber,
        string newText)
    {
        if (newLineNumber == null)
        {
            throw new InvalidOperationException("Inserted lines require a new line number.");
        }

        var oldStart = Math.Max(0, (oldLineNumber ?? newLineNumber.Value) - 1);
        return BuildPatch(path, $"@@ -{oldStart},0 +{newLineNumber.Value},1 @@", null, newText);
    }

    private static string BuildDeletedLinePatch(
        string path,
        int? oldLineNumber,
        int? newLineNumber,
        string oldText)
    {
        if (oldLineNumber == null)
        {
            throw new InvalidOperationException("Deleted lines require an old line number.");
        }

        var newStart = Math.Max(0, (newLineNumber ?? oldLineNumber.Value) - 1);
        return BuildPatch(path, $"@@ -{oldLineNumber.Value},1 +{newStart},0 @@", oldText, null);
    }

    private static string BuildModifiedLinePatch(
        string path,
        int? oldLineNumber,
        int? newLineNumber,
        string oldText,
        string newText)
    {
        if (oldLineNumber == null || newLineNumber == null)
        {
            throw new InvalidOperationException("Modified lines require old and new line numbers.");
        }

        return BuildPatch(path, $"@@ -{oldLineNumber.Value},1 +{newLineNumber.Value},1 @@", oldText, newText);
    }

    private static string BuildPatch(string path, string hunkHeader, string? oldText, string? newText)
    {
        var builder = new StringBuilder(path.Length + (oldText?.Length ?? 0) + (newText?.Length ?? 0) + 128);
        builder.Append("diff --git a/").Append(path).Append(" b/").Append(path).Append('\n');
        builder.Append("--- a/").Append(path).Append('\n');
        builder.Append("+++ b/").Append(path).Append('\n');
        builder.Append(hunkHeader).Append('\n');
        if (oldText != null)
        {
            builder.Append('-').Append(oldText).Append('\n');
        }

        if (newText != null)
        {
            builder.Append('+').Append(newText).Append('\n');
        }

        return builder.ToString();
    }

    private static string? FirstNonEmptyLine(string text)
    {
        foreach (var line in text.AsSpan().EnumerateLines())
        {
            var trimmed = line.Trim();
            if (!trimmed.IsEmpty)
            {
                return trimmed.ToString();
            }
        }

        return null;
    }
}
