using System.Buffers;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.Lfs;

internal static class LfsAttributesReader
{
    private const int BufferSize = 16 * 1024;
    private const int MaximumPatterns = 10_000;

    public static async Task<List<string>> ReadAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(repositoryPath, ".gitattributes");
        if (!File.Exists(path)) return [];
        var patterns = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        await ReadLinesAsync(path, patterns, seen, cancellationToken).ConfigureAwait(false);
        return patterns;
    }

    private static async Task ReadLinesAsync(
        string path,
        List<string> patterns,
        HashSet<string> seen,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<char>.Shared.Rent(BufferSize);
        StringBuilder? pending = null;
        try
        {
            await using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
                BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var reader = new StreamReader(stream);
            int read;
            while (patterns.Count < MaximumPatterns &&
                   (read = await reader.ReadAsync(
                       buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false)) > 0)
            {
                var remaining = buffer.AsSpan(0, read);
                while (patterns.Count < MaximumPatterns)
                {
                    var newline = remaining.IndexOf('\n');
                    if (newline < 0) break;
                    ProcessSegment(remaining[..newline], ref pending, patterns, seen);
                    cancellationToken.ThrowIfCancellationRequested();
                    remaining = remaining[(newline + 1)..];
                }
                if (!remaining.IsEmpty && patterns.Count < MaximumPatterns)
                {
                    pending ??= new StringBuilder(remaining.Length + 128);
                    pending.Append(remaining);
                }
            }
            if (pending is { Length: > 0 } && patterns.Count < MaximumPatterns)
            {
                ProcessLine(pending.ToString(), patterns, seen);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    private static void ProcessSegment(
        ReadOnlySpan<char> segment,
        ref StringBuilder? pending,
        List<string> patterns,
        HashSet<string> seen)
    {
        if (pending == null)
        {
            ProcessLine(segment, patterns, seen);
            return;
        }
        pending.Append(segment);
        ProcessLine(pending.ToString(), patterns, seen);
        pending = null;
    }

    private static void ProcessLine(
        ReadOnlySpan<char> raw,
        List<string> patterns,
        HashSet<string> seen)
    {
        var line = raw.Trim();
        if (line.IsEmpty || line[0] == '#') return;
        var separator = FindPatternEnd(line);
        if (separator <= 0 || !HasLfsFilter(line[separator..].TrimStart())) return;
        var pattern = DecodePattern(line[..separator]);
        if (seen.Add(pattern)) patterns.Add(pattern);
    }

    private static int FindPatternEnd(ReadOnlySpan<char> line)
    {
        var quoted = false;
        var escaped = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (escaped) escaped = false;
            else if (character == '\\') escaped = true;
            else if (character == '"') quoted = !quoted;
            else if (!quoted && char.IsWhiteSpace(character)) return index;
        }
        return -1;
    }

    private static bool HasLfsFilter(ReadOnlySpan<char> attributes)
    {
        while (!attributes.IsEmpty)
        {
            var end = attributes.IndexOfAny(' ', '\t');
            var attribute = end < 0 ? attributes : attributes[..end];
            if (attribute.Equals("filter=lfs", StringComparison.Ordinal)) return true;
            if (end < 0) return false;
            attributes = attributes[(end + 1)..].TrimStart();
        }
        return false;
    }

    private static string DecodePattern(ReadOnlySpan<char> pattern)
    {
        if (pattern.Length >= 2 && pattern[0] == '"' && pattern[^1] == '"')
        {
            pattern = pattern[1..^1];
        }
        var escape = pattern.IndexOf('\\');
        if (escape < 0) return DecodeLfsWhitespace(pattern.ToString());
        var result = new StringBuilder(pattern.Length).Append(pattern[..escape]);
        for (var index = escape; index < pattern.Length; index++)
        {
            var character = pattern[index];
            if (character == '\\' && index + 1 < pattern.Length)
            {
                character = pattern[++index] switch
                {
                    'n' => '\n', 'r' => '\r', 't' => '\t', var value => value,
                };
            }
            result.Append(character);
        }
        return DecodeLfsWhitespace(result.ToString());
    }

    private static string DecodeLfsWhitespace(string pattern) =>
        pattern.Replace("[[:space:]]", " ", StringComparison.Ordinal);
}
