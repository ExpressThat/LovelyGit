using System.Buffers;
using System.Globalization;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

internal static class GitStashReader
{
    public static async Task<IReadOnlyList<GitStashEntry>> ReadAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var logPath = Path.Combine(gitDirectory, "logs", "refs", "stash");
        if (!File.Exists(logPath))
        {
            return Array.Empty<GitStashEntry>();
        }

        var file = new FileInfo(logPath);
        var estimatedCount = (int)Math.Min(file.Length / 128 + 1, 1_000_000);
        var entries = new List<GitStashEntry>(estimatedCount);
        await ReadEntriesAsync(logPath, objectFormat, entries, cancellationToken)
            .ConfigureAwait(false);
        entries.Reverse();
        for (var index = 0; index < entries.Count; index++)
        {
            entries[index] = entries[index] with { Selector = $"stash@{{{index}}}" };
        }

        return entries;
    }

    private static async Task ReadEntriesAsync(
        string path,
        GitObjectFormat objectFormat,
        List<GitStashEntry> entries,
        CancellationToken cancellationToken)
    {
        const int BufferSize = 16 * 1024;
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
                    ProcessSegment(remaining[..newline], ref pending, objectFormat, entries);
                    cancellationToken.ThrowIfCancellationRequested();
                    remaining = remaining[(newline + 1)..];
                }
                if (!remaining.IsEmpty)
                {
                    pending ??= new StringBuilder(remaining.Length + 128);
                    pending.Append(remaining);
                }
            }
            if (pending is { Length: > 0 }) ProcessLine(pending.ToString(), objectFormat, entries);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    private static void ProcessSegment(
        ReadOnlySpan<char> segment,
        ref StringBuilder? pending,
        GitObjectFormat objectFormat,
        List<GitStashEntry> entries)
    {
        if (pending == null)
        {
            ProcessLine(segment, objectFormat, entries);
            return;
        }
        pending.Append(segment);
        ProcessLine(pending.ToString(), objectFormat, entries);
        pending = null;
    }

    private static void ProcessLine(
        ReadOnlySpan<char> line,
        GitObjectFormat objectFormat,
        List<GitStashEntry> entries)
    {
        if (!line.IsEmpty && line[^1] == '\r') line = line[..^1];
        if (TryParse(line, objectFormat, out var entry)) entries.Add(entry);
    }

    private static bool TryParse(
        ReadOnlySpan<char> line,
        GitObjectFormat objectFormat,
        out GitStashEntry entry)
    {
        entry = default;
        var firstSpace = line.IndexOf(' ');
        if (firstSpace < 0)
        {
            return false;
        }

        var afterOldHash = line[(firstSpace + 1)..];
        var secondSpace = afterOldHash.IndexOf(' ');
        if (secondSpace < 0 ||
            !GitObjectId.TryParse(afterOldHash[..secondSpace], objectFormat, out var target))
        {
            return false;
        }

        var metadataAndMessage = afterOldHash[(secondSpace + 1)..];
        var messageSeparator = metadataAndMessage.IndexOf('\t');
        var metadata = messageSeparator >= 0
            ? metadataAndMessage[..messageSeparator]
            : metadataAndMessage;
        var message = messageSeparator >= 0
            ? metadataAndMessage[(messageSeparator + 1)..].Trim().ToString()
            : string.Empty;

        var timezoneSeparator = metadata.LastIndexOf(' ');
        var timestampEnd = timezoneSeparator > 0 ? timezoneSeparator : metadata.Length;
        var timestampStart = metadata[..timestampEnd].LastIndexOf(' ');
        long? createdAtUnixSeconds = null;
        if (timestampStart >= 0 &&
            long.TryParse(
                metadata[(timestampStart + 1)..timestampEnd],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var unixSeconds))
        {
            createdAtUnixSeconds = unixSeconds;
        }

        entry = new GitStashEntry(
            string.Empty,
            target,
            message,
            createdAtUnixSeconds);
        return true;
    }
}

internal readonly record struct GitStashEntry(
    string Selector,
    GitObjectId Target,
    string Message,
    long? CreatedAtUnixSeconds);
