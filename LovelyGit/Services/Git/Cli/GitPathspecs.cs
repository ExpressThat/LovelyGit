using System.Buffers;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal static class GitPathspecs
{
    private static readonly Encoding Encoding = Encoding.UTF8;
    private static readonly byte[] Nul = [0];

    public static IReadOnlyList<string> Normalize(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            throw new InvalidOperationException("At least one file path is required.");
        }

        var unique = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new List<string>(paths.Count);
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            if (Path.IsPathRooted(path) || ContainsUnsafeSegment(path))
            {
                throw new InvalidOperationException("Working tree paths must be repository-relative.");
            }

            var normalizedPath = path.Replace('\\', '/');
            if (unique.Add(normalizedPath))
            {
                normalized.Add(normalizedPath);
            }
        }

        return normalized.Count == 0
            ? throw new InvalidOperationException("At least one file path is required.")
            : normalized;
    }

    public static async Task WriteNullTerminatedAsync(
        Stream stream,
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken)
    {
        foreach (var path in paths)
        {
            var maximumLength = Encoding.GetMaxByteCount(path.Length);
            var buffer = ArrayPool<byte>.Shared.Rent(maximumLength);
            try
            {
                var length = Encoding.GetBytes(path, buffer);
                await stream.WriteAsync(buffer.AsMemory(0, length), cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            await stream.WriteAsync(Nul, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool ContainsUnsafeSegment(ReadOnlySpan<char> path)
    {
        while (!path.IsEmpty)
        {
            var separator = path.IndexOfAny('/', '\\');
            var segment = separator < 0 ? path : path[..separator];
            if (segment.IsEmpty || segment.SequenceEqual(".") || segment.SequenceEqual(".."))
            {
                return true;
            }

            if (separator < 0)
            {
                return false;
            }

            path = path[(separator + 1)..];
        }

        return true;
    }
}
