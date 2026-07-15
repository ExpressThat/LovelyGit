using System.Buffers;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.Patches;

internal static class PatchPreviewParser
{
    private const int BufferSize = 16 * 1024;
    private const int MaxPreviewFiles = 5_000;

    public static async Task ParseAsync(
        StreamReader reader,
        PatchPreviewResponse response,
        CancellationToken cancellationToken)
    {
        var state = new ParseState(response);
        var buffer = ArrayPool<char>.Shared.Rent(BufferSize);
        StringBuilder? pending = null;
        try
        {
            int read;
            while ((read = await reader.ReadAsync(
                       buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false)) > 0)
            {
                var remaining = buffer.AsSpan(0, read);
                while (true)
                {
                    var newline = remaining.IndexOf('\n');
                    if (newline < 0) break;
                    ProcessSegment(state, remaining[..newline], ref pending);
                    remaining = remaining[(newline + 1)..];
                }

                if (!remaining.IsEmpty)
                {
                    pending ??= new StringBuilder(remaining.Length + 128);
                    pending.Append(remaining);
                }
            }

            if (pending is { Length: > 0 })
            {
                state.ProcessLine(TrimCarriageReturn(pending.ToString().AsSpan()));
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    private static void ProcessSegment(
        ParseState state,
        ReadOnlySpan<char> segment,
        ref StringBuilder? pending)
    {
        if (pending == null)
        {
            state.ProcessLine(TrimCarriageReturn(segment));
            return;
        }

        pending.Append(segment);
        state.ProcessLine(TrimCarriageReturn(pending.ToString().AsSpan()));
        pending = null;
    }

    private static ReadOnlySpan<char> TrimCarriageReturn(ReadOnlySpan<char> line) =>
        !line.IsEmpty && line[^1] == '\r' ? line[..^1] : line;

    private sealed class ParseState(PatchPreviewResponse response)
    {
        private PatchFilePreview? _current;
        private string? _oldPath;

        public void ProcessLine(ReadOnlySpan<char> line)
        {
            if (line.StartsWith("--- "))
            {
                _oldPath = ParseHeaderPath(line[4..]);
            }
            else if (line.StartsWith("+++ "))
            {
                _current = StartFile(ParseHeaderPath(line[4..]) ?? _oldPath);
            }
            else if (_current != null && !line.IsEmpty && line[0] == '+')
            {
                _current.Additions++;
                response.TotalAdditions++;
            }
            else if (_current != null && !line.IsEmpty && line[0] == '-')
            {
                _current.Deletions++;
                response.TotalDeletions++;
            }
        }

        private PatchFilePreview? StartFile(string? path)
        {
            if (path == null) return null;
            if (response.Files.Count >= MaxPreviewFiles)
            {
                response.IsTruncated = true;
                return null;
            }

            var preview = new PatchFilePreview { Path = path };
            response.Files.Add(preview);
            return preview;
        }

        private static string? ParseHeaderPath(ReadOnlySpan<char> value)
        {
            var tabIndex = value.IndexOf('\t');
            if (tabIndex >= 0) value = value[..tabIndex];
            if (value.SequenceEqual("/dev/null")) return null;
            if (value.StartsWith("a/") || value.StartsWith("b/")) value = value[2..];
            return value.Trim().ToString();
        }
    }
}
