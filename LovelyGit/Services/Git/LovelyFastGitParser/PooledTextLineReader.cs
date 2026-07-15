using System.Buffers;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal static class PooledTextLineReader
{
    private const int BufferSize = 16 * 1024;

    public static async Task ReadAsync<TState>(
        string path,
        TState state,
        ReadOnlySpanAction<char, TState> processLine,
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
            while ((read = await reader.ReadAsync(
                       buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false)) > 0)
            {
                var remaining = buffer.AsSpan(0, read);
                while (true)
                {
                    var newline = remaining.IndexOf('\n');
                    if (newline < 0) break;
                    ProcessSegment(remaining[..newline], ref pending, state, processLine);
                    cancellationToken.ThrowIfCancellationRequested();
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
                var line = pending.ToString();
                processLine(TrimCarriageReturn(line), state);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    private static void ProcessSegment<TState>(
        ReadOnlySpan<char> segment,
        ref StringBuilder? pending,
        TState state,
        ReadOnlySpanAction<char, TState> processLine)
    {
        if (pending == null)
        {
            processLine(TrimCarriageReturn(segment), state);
            return;
        }
        pending.Append(segment);
        var line = pending.ToString();
        processLine(TrimCarriageReturn(line), state);
        pending = null;
    }

    private static ReadOnlySpan<char> TrimCarriageReturn(ReadOnlySpan<char> line) =>
        !line.IsEmpty && line[^1] == '\r' ? line[..^1] : line;
}
