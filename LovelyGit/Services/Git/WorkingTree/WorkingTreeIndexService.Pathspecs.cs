using System.Buffers;
using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeIndexService
{
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

}
