using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.Bisect;

internal sealed class NativeGitBisectStateReader
{
    public async Task<GitBisectState> ReadAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var startPath = Path.Combine(paths.WorktreeGitDirectory, "BISECT_START");
        if (!File.Exists(startPath)) return new GitBisectState();

        var objectFormat = await GitRepositoryDiscovery
            .ReadObjectFormatAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var currentHash = await GitHeadReader
            .ResolveAsync(
                paths.WorktreeGitDirectory,
                paths.GitDirectory,
                objectFormat,
                cancellationToken)
            .ConfigureAwait(false);
        var currentSubject = currentHash.HasValue
            ? await ReadSubjectAsync(paths.GitDirectory, objectFormat, currentHash.Value, cancellationToken)
                .ConfigureAwait(false)
            : null;
        var (badCommit, goodCommits) = ReadBisectRefs(
            paths.WorktreeGitDirectory, objectFormat, cancellationToken);
        return new GitBisectState
        {
            IsActive = true,
            StartingReference = await ReadTrimmedAsync(startPath, cancellationToken).ConfigureAwait(false),
            CurrentCommit = currentHash?.ToString(),
            CurrentSubject = currentSubject,
            BadCommit = badCommit,
            GoodCommits = goodCommits,
            FirstBadCommit = await ReadFirstBadCommitAsync(
                    Path.Combine(paths.WorktreeGitDirectory, "BISECT_LOG"),
                    cancellationToken)
                .ConfigureAwait(false),
        };
    }

    private static async Task<string> ReadSubjectAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        GitObjectId commitId,
        CancellationToken cancellationToken)
    {
        using var objectStore = new GitObjectStore(gitDirectory, objectFormat);
        var data = await objectStore.ReadObjectAsync(
                commitId, cacheObject: false, cancellationToken)
            .ConfigureAwait(false);
        if (data.Kind != GitObjectKind.Commit)
        {
            throw new InvalidDataException($"Bisect HEAD is not a commit: {commitId}");
        }
        return GitObjectParsers.ParseCommit(commitId, data.Data).Subject;
    }

    private static (string? Bad, List<string> Good) ReadBisectRefs(
        string worktreeGitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var directory = Path.Combine(worktreeGitDirectory, "refs", "bisect");
        if (!Directory.Exists(directory)) return (null, []);
        string? bad = null;
        var good = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(path);
            if (name != "bad" && !name.StartsWith("good-", StringComparison.Ordinal)) continue;
            if (!GitLooseRefReader.TryReadObjectId(path, objectFormat, out var id)) continue;
            if (name == "bad") bad = id.ToString();
            else good.Add(id.ToString());
        }
        return (bad, good.Order(StringComparer.Ordinal).ToList());
    }

    private static async Task<string?> ReadTrimmedAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var value = (await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false)).Trim();
        return value.Length == 0 ? null : value;
    }

    private static async Task<string?> ReadFirstBadCommitAsync(
        string logPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(logPath)) return null;
        const string prefix = "# first bad commit: [";
        string? result = null;
        using var reader = new StreamReader(logPath);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (!line.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var end = line.IndexOf(']', prefix.Length);
            if (end > prefix.Length) result = line[prefix.Length..end];
        }

        return result;
    }
}
