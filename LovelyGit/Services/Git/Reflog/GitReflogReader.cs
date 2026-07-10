using System.Buffers;
using System.Buffers.Text;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Reflog;

internal static class GitReflogReader
{
    private const int BlockSize = 16 * 1024;
    private const int InitialLineSize = 4 * 1024;
    private const int MaximumLineSize = 1024 * 1024;
    private const int MaximumEntries = 500;

    public static async Task<GitReflogResponse> ReadAsync(
        string repositoryPath,
        string? branchName,
        int limit,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var referenceName = string.IsNullOrWhiteSpace(branchName) ? "HEAD" : branchName.Trim();
        var path = ResolveLogPath(paths, branchName);
        var entries = File.Exists(path)
            ? await ReadLatestAsync(
                path,
                referenceName,
                Math.Clamp(limit, 1, MaximumEntries),
                cancellationToken).ConfigureAwait(false)
            : [];
        return new GitReflogResponse
        {
            ReferenceName = referenceName,
            Entries = entries,
        };
    }

    private static string ResolveLogPath(GitRepositoryPaths paths, string? branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return Path.Combine(paths.WorktreeGitDirectory, "logs", "HEAD");
        }

        var branch = branchName.Trim();
        if (!GitBranchNameValidator.IsValidBranchName(branch))
        {
            throw new ArgumentException("Branch name is not valid.", nameof(branchName));
        }

        var relative = branch.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(paths.GitDirectory, "logs", "refs", "heads", relative);
    }

    private static async Task<List<GitReflogEntry>> ReadLatestAsync(
        string path,
        string referenceName,
        int limit,
        CancellationToken cancellationToken)
    {
        var entries = new List<GitReflogEntry>(limit);
        var block = ArrayPool<byte>.Shared.Rent(BlockSize);
        var line = ArrayPool<byte>.Shared.Rent(InitialLineSize);
        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                BlockSize,
                FileOptions.Asynchronous | FileOptions.RandomAccess);
            var position = stream.Length;
            var lineLength = 0;
            while (position > 0 && entries.Count < limit)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var readLength = (int)Math.Min(block.Length, position);
                position -= readLength;
                stream.Position = position;
                await stream.ReadExactlyAsync(block.AsMemory(0, readLength), cancellationToken)
                    .ConfigureAwait(false);
                for (var index = readLength - 1; index >= 0 && entries.Count < limit; index--)
                {
                    if (block[index] == (byte)'\n')
                    {
                        AddReversedLine(entries, line.AsSpan(0, lineLength), referenceName);
                        lineLength = 0;
                        continue;
                    }

                    if (lineLength == line.Length)
                    {
                        line = GrowLineBuffer(line, lineLength);
                    }

                    line[lineLength++] = block[index];
                }
            }

            if (entries.Count < limit && lineLength > 0)
            {
                AddReversedLine(entries, line.AsSpan(0, lineLength), referenceName);
            }

            return entries;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(block);
            ArrayPool<byte>.Shared.Return(line);
        }
    }

    private static byte[] GrowLineBuffer(byte[] current, int length)
    {
        if (current.Length >= MaximumLineSize)
        {
            throw new InvalidDataException("Reflog entry exceeds the supported size.");
        }

        var next = ArrayPool<byte>.Shared.Rent(Math.Min(current.Length * 2, MaximumLineSize));
        current.AsSpan(0, length).CopyTo(next);
        ArrayPool<byte>.Shared.Return(current);
        return next;
    }

    private static void AddReversedLine(
        List<GitReflogEntry> entries,
        Span<byte> reversedLine,
        string referenceName)
    {
        if (reversedLine.IsEmpty)
        {
            return;
        }

        reversedLine.Reverse();
        if (reversedLine[^1] == (byte)'\r')
        {
            reversedLine = reversedLine[..^1];
        }

        if (TryParse(reversedLine, $"{referenceName}@{{{entries.Count}}}", out var entry))
        {
            entries.Add(entry);
        }
    }

    private static bool TryParse(
        ReadOnlySpan<byte> line,
        string selector,
        out GitReflogEntry entry)
    {
        entry = new GitReflogEntry();
        var firstSpace = line.IndexOf((byte)' ');
        var secondSpace = firstSpace < 0 ? -1 : line[(firstSpace + 1)..].IndexOf((byte)' ');
        if (firstSpace <= 0 || secondSpace < 0)
        {
            return false;
        }

        secondSpace += firstSpace + 1;
        var tab = line[(secondSpace + 1)..].IndexOf((byte)'\t');
        if (tab < 0)
        {
            return false;
        }

        tab += secondSpace + 1;
        var metadata = line[(secondSpace + 1)..tab];
        var timezoneSpace = metadata.LastIndexOf((byte)' ');
        var timestampSpace = timezoneSpace <= 0
            ? -1
            : metadata[..timezoneSpace].LastIndexOf((byte)' ');
        if (timestampSpace <= 0 ||
            !Utf8Parser.TryParse(metadata[(timestampSpace + 1)..timezoneSpace], out long timestamp, out _))
        {
            return false;
        }

        var identity = metadata[..timestampSpace].Trim((byte)' ');
        var emailStart = identity.LastIndexOf((byte)'<');
        var emailEnd = identity.LastIndexOf((byte)'>');
        var name = emailStart > 0 ? identity[..emailStart].Trim((byte)' ') : identity;
        var email = emailStart >= 0 && emailEnd > emailStart
            ? identity[(emailStart + 1)..emailEnd]
            : ReadOnlySpan<byte>.Empty;
        var oldHash = line[..firstSpace];
        var newHash = line[(firstSpace + 1)..secondSpace];
        if (!IsObjectId(oldHash) || !IsObjectId(newHash))
        {
            return false;
        }

        entry = new GitReflogEntry
        {
            Selector = selector,
            OldHash = Encoding.ASCII.GetString(oldHash),
            NewHash = Encoding.ASCII.GetString(newHash),
            ActorName = Encoding.UTF8.GetString(name),
            ActorEmail = Encoding.UTF8.GetString(email),
            TimestampUnixSeconds = timestamp,
            Timezone = Encoding.ASCII.GetString(metadata[(timezoneSpace + 1)..]),
            Message = Encoding.UTF8.GetString(line[(tab + 1)..]),
        };
        return true;
    }

    private static bool IsObjectId(ReadOnlySpan<byte> value) =>
        value.Length is 40 or 64 && value.IndexOfAnyExcept("0123456789abcdefABCDEF"u8) < 0;
}
