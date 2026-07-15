using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal static class GitRepositoryDiscovery
{
    public static async Task<GitRepositoryPaths> ResolveRepositoryPathsAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        var attributes = File.GetAttributes(fullPath);
        if ((attributes & FileAttributes.Directory) == 0)
        {
            throw new DirectoryNotFoundException($"Path is not a directory: {path}");
        }

        if (Path.GetFileName(fullPath).Equals(".git", StringComparison.OrdinalIgnoreCase))
        {
            return await CreatePathsAsync(
                fullPath,
                Directory.GetParent(fullPath)?.FullName ?? fullPath,
                cancellationToken).ConfigureAwait(false);
        }

        var dotGitPath = Path.Combine(fullPath, ".git");
        if (Directory.Exists(dotGitPath))
        {
            return await CreatePathsAsync(dotGitPath, fullPath, cancellationToken).ConfigureAwait(false);
        }

        if (File.Exists(dotGitPath))
        {
            var text = (await File.ReadAllTextAsync(dotGitPath, cancellationToken).ConfigureAwait(false)).Trim();
            const string prefix = "gitdir:";
            if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(".git file does not contain a gitdir pointer.");
            }

            var gitDir = text.AsSpan(prefix.Length).Trim().ToString();
            var worktreeGitDirectory = Path.GetFullPath(
                Path.IsPathRooted(gitDir) ? gitDir : Path.Combine(fullPath, gitDir));
            return await CreatePathsAsync(worktreeGitDirectory, fullPath, cancellationToken)
                .ConfigureAwait(false);
        }

        throw new DirectoryNotFoundException($"Could not find .git directory for: {path}");
    }

    public static async Task<string> ResolveGitDirectoryAsync(string path, CancellationToken cancellationToken)
    {
        return (await ResolveRepositoryPathsAsync(path, cancellationToken).ConfigureAwait(false)).GitDirectory;
    }

    private static async Task<GitRepositoryPaths> CreatePathsAsync(
        string worktreeGitDirectory,
        string workTreeDirectory,
        CancellationToken cancellationToken)
    {
        var commonDirectory = worktreeGitDirectory;
        var commonDirPath = Path.Combine(worktreeGitDirectory, "commondir");
        if (File.Exists(commonDirPath))
        {
            var value = (await File.ReadAllTextAsync(commonDirPath, cancellationToken)
                    .ConfigureAwait(false))
                .AsSpan()
                .Trim();
            if (value.Length > 0)
            {
                var path = value.ToString();
                commonDirectory = Path.GetFullPath(
                    Path.IsPathRooted(path)
                        ? path
                        : Path.Combine(worktreeGitDirectory, path));
            }
        }

        return new GitRepositoryPaths(
            commonDirectory,
            worktreeGitDirectory,
            workTreeDirectory);
    }

    public static async Task<GitObjectFormat> ReadObjectFormatAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(gitDirectory, "config");
        if (!File.Exists(configPath))
        {
            return GitObjectFormat.Sha1;
        }

        if (TryReadSmallObjectFormat(configPath, cancellationToken, out var smallFormat))
        {
            return smallFormat;
        }

        var state = new ObjectFormatParseState();
        await GitConfigLineReader.ReadAsync(
                configPath,
                state,
                static (line, parseState) => parseState.ProcessLine(line),
                cancellationToken)
            .ConfigureAwait(false);
        return state.Format ?? GitObjectFormat.Sha1;
    }

    private static bool TryReadSmallObjectFormat(
        string path,
        CancellationToken cancellationToken,
        out GitObjectFormat format)
    {
        const int maximumBytes = 8 * 1024;
        cancellationToken.ThrowIfCancellationRequested();
        Span<byte> bytes = stackalloc byte[maximumBytes];
        using var handle = File.OpenHandle(
            path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete, FileOptions.SequentialScan);
        var length = 0;
        while (length < bytes.Length)
        {
            var read = RandomAccess.Read(handle, bytes[length..], length);
            if (read == 0) break;
            length += read;
        }
        Span<byte> overflow = stackalloc byte[1];
        if (length == bytes.Length && RandomAccess.Read(handle, overflow, length) != 0)
        {
            format = default;
            return false;
        }

        var state = new ObjectFormatParseState();
        var text = Encoding.UTF8.GetString(bytes[..length]);
        var remaining = text.AsSpan().TrimStart('\uFEFF');
        while (true)
        {
            var newline = remaining.IndexOf('\n');
            if (newline < 0) break;
            state.ProcessLine(remaining[..newline]);
            remaining = remaining[(newline + 1)..];
        }
        if (!remaining.IsEmpty) state.ProcessLine(remaining);
        format = state.Format ?? GitObjectFormat.Sha1;
        return true;
    }

    private sealed class ObjectFormatParseState
    {
        private bool _inExtensions;
        public GitObjectFormat? Format { get; private set; }

        public void ProcessLine(ReadOnlySpan<char> rawLine)
        {
            if (Format != null) return;
            var line = rawLine.Trim();
            if (line.IsEmpty || line[0] is '#' or ';') return;
            if (line[0] == '[' && line[^1] == ']')
            {
                _inExtensions = line[1..^1].Trim().Trim('"')
                    .Equals("extensions", StringComparison.OrdinalIgnoreCase);
                return;
            }
            if (!_inExtensions) return;
            var separator = line.IndexOf('=');
            if (separator <= 0 || !line[..separator].Trim()
                    .Equals("objectformat", StringComparison.OrdinalIgnoreCase)) return;
            var value = line[(separator + 1)..].Trim().Trim('"');
            Format = value.Equals("sha1", StringComparison.OrdinalIgnoreCase)
                ? GitObjectFormat.Sha1
                : value.Equals("sha256", StringComparison.OrdinalIgnoreCase)
                    ? GitObjectFormat.Sha256
                    : throw new NotSupportedException($"Unsupported Git object format: {value.ToString()}");
        }
    }
}

internal sealed record GitRepositoryPaths(
    string GitDirectory,
    string WorktreeGitDirectory,
    string WorkTreeDirectory);
