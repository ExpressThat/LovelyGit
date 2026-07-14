using System.Buffers.Binary;
using System.Text;

namespace LovelyGit.Tests.Git.WorkingTree;

internal static class SyntheticGitIndexWriter
{
    public static void WriteVersion2(string path, int entryCount)
    {
        using var stream = File.Create(path);
        Span<byte> header = stackalloc byte[12];
        "DIRC"u8.CopyTo(header);
        BinaryPrimitives.WriteUInt32BigEndian(header.Slice(4, 4), 2);
        BinaryPrimitives.WriteUInt32BigEndian(header.Slice(8, 4), (uint)entryCount);
        stream.Write(header);

        Span<byte> fixedBytes = stackalloc byte[62];
        for (var index = 0; index < entryCount; index++)
        {
            fixedBytes.Clear();
            BinaryPrimitives.WriteUInt32BigEndian(fixedBytes.Slice(24, 4), 0x81A4);
            BinaryPrimitives.WriteUInt32BigEndian(fixedBytes.Slice(36, 4), 128);
            BinaryPrimitives.WriteInt32BigEndian(fixedBytes.Slice(56, 4), index);
            var entryPath = Encoding.UTF8.GetBytes($"src/file-{index:D6}.txt");
            BinaryPrimitives.WriteUInt16BigEndian(
                fixedBytes.Slice(60, 2),
                (ushort)entryPath.Length);
            stream.Write(fixedBytes);
            stream.Write(entryPath);
            stream.WriteByte(0);
            var padding = (8 - (fixedBytes.Length + entryPath.Length + 1) % 8) % 8;
            for (var count = 0; count < padding; count++)
            {
                stream.WriteByte(0);
            }
        }
    }
}
