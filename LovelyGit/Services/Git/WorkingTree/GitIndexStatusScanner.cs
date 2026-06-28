using System.Buffers.Binary;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class GitIndexStatusScanner
{
    public async Task<GitIndexStatusScan> ScanAsync(
        string gitDirectory,
        string workTreeDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken,
        bool includeTrackedChanges = true,
        bool collectRootTracking = true)
    {
        var indexPath = Path.Combine(gitDirectory, "index");
        if (!File.Exists(indexPath))
        {
            return new GitIndexStatusScan(new WorkingTreeChangesResponse(), null, [], []);
        }

        var bytes = await File.ReadAllBytesAsync(indexPath, cancellationToken).ConfigureAwait(false);
        var length = bytes.Length;
        var result = Scan(
            bytes,
            workTreeDirectory,
            objectFormat,
            cancellationToken,
            includeTrackedChanges,
            collectRootTracking);
        bytes = [];
        GitIndexMemory.ReleaseLargeBuffer(length);
        return result;
    }

    private static GitIndexStatusScan Scan(
        byte[] bytes,
        string workTreeDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken,
        bool includeTrackedChanges,
        bool collectRootTracking)
    {
        if (bytes.Length < 12 || !bytes.AsSpan(0, 4).SequenceEqual("DIRC"u8))
        {
            throw new InvalidDataException("Git index header is invalid.");
        }

        var version = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(4, 4));
        if (version is not (2 or 3 or 4))
        {
            throw new NotSupportedException($"Unsupported Git index version: {version}");
        }

        var count = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(8, 4));
        var hashLength = GitObjectId.GetByteLength(objectFormat);
        var response = new WorkingTreeChangesResponse();
        var rootTrackedFiles = new HashSet<string>(StringComparer.Ordinal);
        var rootTrackedDirectories = new HashSet<string>(StringComparer.Ordinal);
        var offset = 12;
        var previousPath = string.Empty;
        string? previousUnmergedPath = null;

        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = ReadEntry(bytes, version, hashLength, ref offset, previousPath);
            previousPath = entry.Path;
            if (collectRootTracking)
            {
                AddRootTracking(entry.Path, rootTrackedFiles, rootTrackedDirectories);
            }
            if (entry.Stage == 0)
            {
                if (includeTrackedChanges)
                {
                    AddWorkTreeChange(response, entry, workTreeDirectory);
                }

                continue;
            }

            if (!string.Equals(previousUnmergedPath, entry.Path, StringComparison.Ordinal))
            {
                response.Unmerged.Add(Create(entry.Path, "Unmerged", WorkingTreeChangeGroup.Unmerged));
                previousUnmergedPath = entry.Path;
            }
        }

        return new GitIndexStatusScan(
            response,
            TryReadCacheTreeRootId(bytes, offset, hashLength, objectFormat),
            rootTrackedFiles,
            rootTrackedDirectories);
    }

    private static GitIndexStatusEntry ReadEntry(
        byte[] bytes,
        uint version,
        int hashLength,
        ref int offset,
        string previousPath)
    {
        if (offset + 62 + hashLength - 20 > bytes.Length)
        {
            throw new InvalidDataException("Git index entry is truncated.");
        }

        var entryStart = offset;
        offset += 8;
        var mtimeSeconds = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
        offset += 8;
        offset += 20;
        var fileSize = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
        offset += 4 + hashLength;
        var flags = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
        offset += 2;
        ushort extendedFlags = 0;
        if (version >= 3 && (flags & 0x4000) != 0)
        {
            extendedFlags = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
            offset += 2;
        }

        var path = version == 4
            ? ReadVersion4Path(bytes, ref offset, previousPath)
            : ReadPaddedPath(bytes, ref offset, entryStart, flags);
        return new GitIndexStatusEntry(
            path.Replace('\\', '/'),
            (flags >> 12) & 0x3,
            fileSize,
            DateTimeOffset.FromUnixTimeSeconds(mtimeSeconds),
            (extendedFlags & 0x4000) != 0,
            (extendedFlags & 0x2000) != 0);
    }

    private static string ReadVersion4Path(byte[] bytes, ref int offset, string previousPath)
    {
        var prefixLength = ReadIndexVarInt(bytes, ref offset);
        var end = Array.IndexOf(bytes, (byte)0, offset);
        if (end < 0)
        {
            throw new InvalidDataException("Git index v4 path is unterminated.");
        }

        var suffix = Encoding.UTF8.GetString(bytes, offset, end - offset);
        offset = end + 1;
        return string.Concat(previousPath.AsSpan(0, prefixLength), suffix);
    }

    private static string ReadPaddedPath(byte[] bytes, ref int offset, int entryStart, ushort flags)
    {
        var nameLength = flags & 0x0fff;
        int end;
        if (nameLength < 0x0fff)
        {
            end = offset + nameLength;
        }
        else
        {
            end = Array.IndexOf(bytes, (byte)0, offset);
            if (end < 0)
            {
                throw new InvalidDataException("Git index path is unterminated.");
            }
        }

        var path = Encoding.UTF8.GetString(bytes, offset, end - offset);
        offset = end + 1;
        var paddedLength = ((offset - entryStart + 7) / 8) * 8;
        offset = entryStart + paddedLength;
        return path;
    }

    private static int ReadIndexVarInt(byte[] bytes, ref int offset)
    {
        var value = 0;
        byte current;
        do
        {
            current = bytes[offset++];
            value = (value << 7) + (current & 0x7f);
            if ((current & 0x80) != 0)
            {
                value++;
            }
        } while ((current & 0x80) != 0);

        return value;
    }

    private static void AddWorkTreeChange(
        WorkingTreeChangesResponse response,
        GitIndexStatusEntry entry,
        string workTreeDirectory)
    {
        if (entry.SkipWorkTree || entry.IntentToAdd)
        {
            return;
        }

        var path = Path.Combine(workTreeDirectory, entry.Path.Replace('/', Path.DirectorySeparatorChar));
        FileInfo info;
        try
        {
            info = new FileInfo(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            response.Unstaged.Add(Create(entry.Path, "Modified", WorkingTreeChangeGroup.Unstaged));
            return;
        }

        if (!info.Exists)
        {
            response.Unstaged.Add(Create(entry.Path, "Deleted", WorkingTreeChangeGroup.Unstaged));
            return;
        }

        if (entry.FileSize != info.Length
            || Math.Abs((info.LastWriteTimeUtc - entry.ModifiedTime.UtcDateTime).TotalSeconds) >= 1)
        {
            response.Unstaged.Add(Create(entry.Path, "Modified", WorkingTreeChangeGroup.Unstaged));
        }
    }

    private static WorkingTreeChangedFile Create(
        string path,
        string status,
        WorkingTreeChangeGroup group) =>
        new()
        {
            Path = path,
            Status = status,
            Group = group,
        };
}
