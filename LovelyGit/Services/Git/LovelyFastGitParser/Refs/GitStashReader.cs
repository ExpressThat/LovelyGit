using System.Globalization;

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

        var lines = await File.ReadAllLinesAsync(logPath, cancellationToken)
            .ConfigureAwait(false);
        var entries = new List<GitStashEntry>(lines.Length);
        for (var lineIndex = lines.Length - 1; lineIndex >= 0; lineIndex--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryParse(lines[lineIndex].AsSpan(), objectFormat, entries.Count, out var entry))
            {
                continue;
            }

            entries.Add(entry);
        }

        return entries;
    }

    private static bool TryParse(
        ReadOnlySpan<char> line,
        GitObjectFormat objectFormat,
        int stashIndex,
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
            $"stash@{{{stashIndex}}}",
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
