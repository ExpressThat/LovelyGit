using System.Buffers.Binary;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class GitIndexRootTracker
{
    public async Task<GitIndexRootTracking> ReadAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var indexPath = Path.Combine(gitDirectory, "index");
        if (!File.Exists(indexPath))
        {
            return new GitIndexRootTracking([], []);
        }

        var bytes = await File.ReadAllBytesAsync(indexPath, cancellationToken).ConfigureAwait(false);
        var length = bytes.Length;
        var result = Read(bytes, objectFormat, cancellationToken);
        bytes = [];
        GitIndexMemory.ReleaseLargeBuffer(length);
        return result;
    }

    private static GitIndexRootTracking Read(
        byte[] bytes,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
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
        var files = new HashSet<string>(StringComparer.Ordinal);
        var directories = new HashSet<string>(StringComparer.Ordinal);
        var previousPath = string.Empty;
        var offset = 12;

        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entryStart = offset;
            offset += 40 + hashLength;
            var flags = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
            offset += 2;
            if (version >= 3 && (flags & 0x4000) != 0)
            {
                offset += 2;
            }

            var path = version == 4
                ? ReadVersion4Path(bytes, ref offset, previousPath)
                : ReadPaddedPath(bytes, ref offset, entryStart, flags);
            previousPath = path;
            AddRoot(path.Replace('\\', '/'), files, directories);
        }

        return new GitIndexRootTracking(files, directories);
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
        offset = entryStart + (((end + 1 - entryStart + 7) / 8) * 8);
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

    private static void AddRoot(
        string path,
        HashSet<string> files,
        HashSet<string> directories)
    {
        var slash = path.IndexOf('/');
        if (slash < 0)
        {
            files.Add(path);
            return;
        }

        directories.Add(path[..slash]);
    }
}

internal sealed record GitIndexRootTracking(
    HashSet<string> RootTrackedFiles,
    HashSet<string> RootTrackedDirectories);
