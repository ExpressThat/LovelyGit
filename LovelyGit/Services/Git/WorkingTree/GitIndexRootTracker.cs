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

        await using var file = File.Open(indexPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var header = await ReadBytesAsync(file, 12, cancellationToken).ConfigureAwait(false);
        if (!header.AsSpan(0, 4).SequenceEqual("DIRC"u8))
        {
            throw new InvalidDataException("Git index header is invalid.");
        }

        var version = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(4, 4));
        if (version is not (2 or 3 or 4))
        {
            throw new NotSupportedException($"Unsupported Git index version: {version}");
        }

        var count = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(8, 4));
        var hashLength = GitObjectId.GetByteLength(objectFormat);
        var files = new HashSet<string>(StringComparer.Ordinal);
        var directories = new HashSet<string>(StringComparer.Ordinal);
        var previousPath = string.Empty;

        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entryStart = file.Position;
            await SkipBytesAsync(file, 40 + hashLength, cancellationToken).ConfigureAwait(false);
            var flagsBytes = await ReadBytesAsync(file, 2, cancellationToken).ConfigureAwait(false);
            var flags = BinaryPrimitives.ReadUInt16BigEndian(flagsBytes);
            if (version >= 3 && (flags & 0x4000) != 0)
            {
                await SkipBytesAsync(file, 2, cancellationToken).ConfigureAwait(false);
            }

            var path = version == 4
                ? ReadVersion4Path(file, previousPath)
                : ReadPaddedPath(file, entryStart);
            previousPath = path;
            AddRoot(path.Replace('\\', '/'), files, directories);
        }

        return new GitIndexRootTracking(files, directories);
    }

    private static string ReadVersion4Path(
        FileStream file,
        string previousPath)
    {
        var prefixLength = ReadIndexVarInt(file);
        var suffix = ReadNulTerminatedString(file);
        return string.Concat(previousPath.AsSpan(0, prefixLength), suffix);
    }

    private static string ReadPaddedPath(
        FileStream file,
        long entryStart)
    {
        var path = ReadNulTerminatedString(file);
        var paddedLength = ((file.Position - entryStart + 7) / 8) * 8;
        file.Position = entryStart + paddedLength;
        return path;
    }

    private static string ReadNulTerminatedString(FileStream file)
    {
        var bytes = new List<byte>(128);
        while (true)
        {
            var value = file.ReadByte();
            if (value < 0)
            {
                throw new InvalidDataException("Git index path is unterminated.");
            }

            if (value == 0)
            {
                return Encoding.UTF8.GetString(bytes.ToArray());
            }

            bytes.Add((byte)value);
        }
    }

    private static int ReadIndexVarInt(FileStream file)
    {
        var value = 0;
        byte current;
        do
        {
            var next = file.ReadByte();
            if (next < 0)
            {
                throw new InvalidDataException("Git index varint is truncated.");
            }

            current = (byte)next;
            value = (value << 7) + (current & 0x7f);
            if ((current & 0x80) != 0)
            {
                value++;
            }
        } while ((current & 0x80) != 0);

        return value;
    }

    private static async Task<byte[]> ReadBytesAsync(
        FileStream file,
        int count,
        CancellationToken cancellationToken)
    {
        var bytes = new byte[count];
        await file.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
        return bytes;
    }

    private static async Task SkipBytesAsync(
        FileStream file,
        int count,
        CancellationToken cancellationToken)
    {
        if (file.CanSeek)
        {
            file.Position += count;
            return;
        }

        _ = await ReadBytesAsync(file, count, cancellationToken).ConfigureAwait(false);
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
