using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class GitIgnoreService
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task<GitIgnoreResult> AddExactPathAsync(
        string repositoryPath,
        string relativePath,
        GitIgnoreTarget target,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(target))
        {
            throw new ArgumentOutOfRangeException(nameof(target));
        }

        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var normalizedPath = NormalizeRepositoryPath(paths.WorkTreeDirectory, relativePath);
        var pattern = BuildExactPattern(normalizedPath);
        var targetPath = target == GitIgnoreTarget.Local
            ? Path.Combine(paths.GitDirectory, "info", "exclude")
            : Path.Combine(paths.WorkTreeDirectory, ".gitignore");

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            var added = await AppendIfMissingAsync(targetPath, pattern, cancellationToken)
                .ConfigureAwait(false);
            return new GitIgnoreResult
            {
                Added = added,
                Pattern = pattern,
                Target = target,
            };
        }
        finally
        {
            _writeLock.Release();
        }
    }

    internal static string BuildExactPattern(string relativePath)
    {
        var builder = new StringBuilder(relativePath.Length + 1).Append('/');
        foreach (var character in relativePath)
        {
            if (character is '*' or '?' or '[' or ']' or '\\' or ' ')
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static string NormalizeRepositoryPath(string repositoryPath, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            relativePath.ContainsAny(['\r', '\n']) ||
            Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("A repository-relative path is required.", nameof(relativePath));
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryPath));
        var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
        var rootPrefix = root + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsLinux()
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
        if (!candidate.StartsWith(rootPrefix, comparison))
        {
            throw new ArgumentException("The ignored path must stay inside the repository.", nameof(relativePath));
        }

        var normalized = Path.GetRelativePath(root, candidate);
        if (Path.DirectorySeparatorChar == '\\') normalized = normalized.Replace('\\', '/');
        if (normalized.Equals(".git", comparison) || normalized.StartsWith(".git/", comparison))
        {
            throw new ArgumentException("Git metadata cannot be ignored.", nameof(relativePath));
        }

        return normalized;
    }

    private static async Task<bool> AppendIfMissingAsync(
        string path,
        string pattern,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: true))
        {
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            {
                if (line.Equals(pattern, StringComparison.Ordinal)) return false;
            }
        }

        var needsNewLine = stream.Length > 0 && !EndsWithNewLine(stream);
        stream.Seek(0, SeekOrigin.End);
        var text = $"{(needsNewLine ? Environment.NewLine : string.Empty)}{pattern}{Environment.NewLine}";
        await stream.WriteAsync(Encoding.UTF8.GetBytes(text), cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static bool EndsWithNewLine(FileStream stream)
    {
        stream.Seek(-1, SeekOrigin.End);
        return stream.ReadByte() is '\n' or '\r';
    }
}
