using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Worktrees;

internal static class GitWorktreeReader
{
    public static async Task<IReadOnlyList<GitWorktree>> ReadAsync(
        string gitDirectory,
        string currentWorkTreeDirectory,
        CancellationToken cancellationToken)
    {
        var commonGitDirectory = await ResolveCommonGitDirectoryAsync(gitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var worktrees = new List<GitWorktree>
        {
            await ReadWorktreeAsync(
                    gitDirectory,
                    currentWorkTreeDirectory,
                    currentWorkTreeDirectory,
                    isCurrent: true,
                    cancellationToken)
                .ConfigureAwait(false),
        };

        var worktreesDirectory = Path.Combine(commonGitDirectory, "worktrees");
        if (Directory.Exists(worktreesDirectory))
        {
            var adminDirectories = Directory.GetDirectories(worktreesDirectory);
            if (adminDirectories.Length < 4)
            {
                foreach (var adminDirectory in adminDirectories)
                {
                    var linked = await ReadLinkedWorktreeAsync(
                            adminDirectory, currentWorkTreeDirectory, cancellationToken)
                        .ConfigureAwait(false);
                    if (linked != null)
                    {
                        worktrees.Add(linked);
                    }
                }
            }
            else
            {
                var linked = new GitWorktree?[adminDirectories.Length];
                await Parallel.ForAsync(
                    0,
                    adminDirectories.Length,
                    new ParallelOptions
                    {
                        CancellationToken = cancellationToken,
                        MaxDegreeOfParallelism = 8,
                    },
                    async (index, token) =>
                    {
                        linked[index] = await ReadLinkedWorktreeAsync(
                                adminDirectories[index], currentWorkTreeDirectory, token)
                            .ConfigureAwait(false);
                    }).ConfigureAwait(false);
                foreach (var worktree in linked)
                {
                    if (worktree != null)
                    {
                        worktrees.Add(worktree);
                    }
                }
            }
        }

        return worktrees
            .OrderByDescending(worktree => worktree.IsCurrent)
            .ThenBy(worktree => worktree.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static async Task<GitWorktree?> ReadLinkedWorktreeAsync(
        string adminDirectory,
        string currentWorkTreeDirectory,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var worktreePath = await ReadWorktreePathAsync(adminDirectory, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(worktreePath) ||
            IsSamePath(worktreePath, currentWorkTreeDirectory))
        {
            return null;
        }

        return await ReadWorktreeAsync(
                adminDirectory,
                worktreePath,
                currentWorkTreeDirectory,
                isCurrent: false,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<GitWorktree> ReadWorktreeAsync(
        string gitDirectory,
        string worktreePath,
        string currentWorkTreeDirectory,
        bool isCurrent,
        CancellationToken cancellationToken)
    {
        var branchName = await GitRefReader
            .ResolveHeadBranchNameAsync(gitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var lockPath = Path.Combine(gitDirectory, "locked");
        var lockReason = File.Exists(lockPath)
            ? (await File.ReadAllTextAsync(lockPath, cancellationToken).ConfigureAwait(false)).Trim()
            : string.Empty;

        return new GitWorktree(
            Path.GetFullPath(worktreePath),
            branchName,
            isCurrent || IsSamePath(worktreePath, currentWorkTreeDirectory),
            File.Exists(lockPath),
            lockReason);
    }

    private static async Task<string> ResolveCommonGitDirectoryAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var commonDirPath = Path.Combine(gitDirectory, "commondir");
        if (!File.Exists(commonDirPath))
        {
            return gitDirectory;
        }

        var text = (await File.ReadAllTextAsync(commonDirPath, cancellationToken).ConfigureAwait(false))
            .Trim();
        return Path.GetFullPath(Path.IsPathRooted(text) ? text : Path.Combine(gitDirectory, text));
    }

    private static async Task<string> ReadWorktreePathAsync(
        string adminDirectory,
        CancellationToken cancellationToken)
    {
        var gitDirPath = Path.Combine(adminDirectory, "gitdir");
        if (!File.Exists(gitDirPath))
        {
            return string.Empty;
        }

        var gitFilePath = (await File.ReadAllTextAsync(gitDirPath, cancellationToken).ConfigureAwait(false))
            .Trim();
        return Path.GetDirectoryName(gitFilePath) ?? string.Empty;
    }

    private static bool IsSamePath(string left, string right) =>
        Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Equals(
                Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
}
