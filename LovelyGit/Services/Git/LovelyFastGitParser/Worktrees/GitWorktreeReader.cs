using System.Text;

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
            ReadWorktree(
                gitDirectory,
                currentWorkTreeDirectory,
                currentWorkTreeDirectory,
                isCurrent: true,
                cancellationToken),
        };

        var worktreesDirectory = Path.Combine(commonGitDirectory, "worktrees");
        if (Directory.Exists(worktreesDirectory))
        {
            var adminDirectories = Directory.GetDirectories(worktreesDirectory);
            if (adminDirectories.Length < 4)
            {
                foreach (var adminDirectory in adminDirectories)
                {
                    var linked = ReadLinkedWorktree(
                        adminDirectory, currentWorkTreeDirectory, cancellationToken);
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
                    (index, token) =>
                    {
                        linked[index] = ReadLinkedWorktree(
                            adminDirectories[index], currentWorkTreeDirectory, token);
                        return ValueTask.CompletedTask;
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

    private static GitWorktree? ReadLinkedWorktree(
        string adminDirectory,
        string currentWorkTreeDirectory,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var worktreePath = ReadWorktreePath(adminDirectory, cancellationToken);
        if (string.IsNullOrWhiteSpace(worktreePath) ||
            IsSamePath(worktreePath, currentWorkTreeDirectory))
        {
            return null;
        }

        return ReadWorktree(
            adminDirectory,
            worktreePath,
            currentWorkTreeDirectory,
            isCurrent: false,
            cancellationToken);
    }

    private static GitWorktree ReadWorktree(
        string gitDirectory,
        string worktreePath,
        string currentWorkTreeDirectory,
        bool isCurrent,
        CancellationToken cancellationToken)
    {
        var head = ReadTrimmed(Path.Combine(gitDirectory, "HEAD"), cancellationToken);
        const string HeadPrefix = "ref: refs/heads/";
        var branchName = head.StartsWith(HeadPrefix, StringComparison.Ordinal)
            ? head[HeadPrefix.Length..]
            : null;
        var lockPath = Path.Combine(gitDirectory, "locked");
        var lockReason = ReadTrimmed(lockPath, cancellationToken);

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

    private static string ReadWorktreePath(
        string adminDirectory,
        CancellationToken cancellationToken)
    {
        var gitDirPath = Path.Combine(adminDirectory, "gitdir");
        if (!File.Exists(gitDirPath))
        {
            return string.Empty;
        }

        var gitFilePath = ReadTrimmed(gitDirPath, cancellationToken);
        return Path.GetDirectoryName(gitFilePath) ?? string.Empty;
    }

    private static string ReadTrimmed(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return string.Empty;
        cancellationToken.ThrowIfCancellationRequested();
        Span<byte> buffer = stackalloc byte[4 * 1024];
        using var handle = File.OpenHandle(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            FileOptions.SequentialScan);
        var length = RandomAccess.Read(handle, buffer, 0);
        Span<byte> overflow = stackalloc byte[1];
        if (length == buffer.Length && RandomAccess.Read(handle, overflow, length) != 0)
        {
            var text = File.ReadAllText(path).Trim();
            cancellationToken.ThrowIfCancellationRequested();
            return text;
        }
        return Encoding.UTF8.GetString(TrimUtf8(buffer[..length]));
    }

    private static ReadOnlySpan<byte> TrimUtf8(ReadOnlySpan<byte> value)
    {
        if (value.Length >= 3 && value[0] == 0xef && value[1] == 0xbb && value[2] == 0xbf)
        {
            value = value[3..];
        }
        var start = 0;
        while (start < value.Length && IsAsciiWhitespace(value[start])) start++;
        var end = value.Length;
        while (end > start && IsAsciiWhitespace(value[end - 1])) end--;
        return value[start..end];
    }

    private static bool IsAsciiWhitespace(byte value) =>
        value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n' or 0x0b or 0x0c;

    private static bool IsSamePath(string left, string right) =>
        Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Equals(
                Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
}
