using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using System.Buffers;
using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeWatcherService : IDisposable
{
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WorkingTreeWatcherService));
        }
    }

    private static ulong ComputeCommitGraphSnapshot(string gitDirectory)
    {
        ulong xor = 0;
        ulong sum = 0;
        ulong count = 0;
        foreach (var path in EnumerateCommitGraphSnapshotPaths(gitDirectory))
        {
            var itemHash = ComputeCommitGraphSnapshotItem(gitDirectory, path);
            xor ^= itemHash;
            sum += itemHash;
            count++;
        }

        var hash = FnvOffsetBasis;
        AddUInt64ToHash(ref hash, xor);
        AddUInt64ToHash(ref hash, sum);
        AddUInt64ToHash(ref hash, count);
        return hash;
    }

    private static IEnumerable<string> EnumerateCommitGraphSnapshotPaths(string gitDirectory)
    {
        var headPath = Path.Combine(gitDirectory, "HEAD");
        if (File.Exists(headPath))
        {
            yield return headPath;
        }

        var packedRefsPath = Path.Combine(gitDirectory, "packed-refs");
        if (File.Exists(packedRefsPath))
        {
            yield return packedRefsPath;
        }

        var refsDirectory = Path.Combine(gitDirectory, "refs");
        if (!Directory.Exists(refsDirectory))
        {
            yield break;
        }

        foreach (var path in Directory.EnumerateFiles(refsDirectory, "*", SearchOption.AllDirectories))
        {
            yield return path;
        }
    }

    private static ulong ComputeCommitGraphSnapshotItem(string gitDirectory, string path)
    {
        var hash = FnvOffsetBasis;
        AddStringToHash(ref hash, Path.GetRelativePath(gitDirectory, path).Replace('\\', '/'));
        AddByteToHash(ref hash, 0);
        AddFileToHash(ref hash, path);
        return hash;
    }

    private static void AddFileToHash(ref ulong hash, string path)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (var index = 0; index < bytesRead; index++)
                {
                    AddByteToHash(ref hash, buffer[index]);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            try
            {
                var info = new FileInfo(path);
                AddUInt64ToHash(ref hash, unchecked((ulong)info.Length));
                AddUInt64ToHash(ref hash, unchecked((ulong)info.LastWriteTimeUtc.Ticks));
            }
            catch (Exception innerException) when (innerException is IOException or UnauthorizedAccessException)
            {
                AddStringToHash(ref hash, path);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void AddStringToHash(ref ulong hash, string value)
    {
        foreach (var character in value.AsSpan())
        {
            AddByteToHash(ref hash, (byte)character);
            AddByteToHash(ref hash, (byte)(character >> 8));
        }
    }

    private static void AddByteToHash(ref ulong hash, byte value)
    {
        hash ^= value;
        hash *= FnvPrime;
    }

    private static void AddUInt64ToHash(ref ulong hash, ulong value)
    {
        for (var shift = 0; shift < 64; shift += 8)
        {
            AddByteToHash(ref hash, (byte)(value >> shift));
        }
    }
}
