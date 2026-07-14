using System.Security.Cryptography;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class ConflictWorktreeSnapshotReader
{
    private const int MaximumAttempts = 2;

    public static async Task<ConflictWorktreeSnapshot> ReadAsync(
        string path,
        int maximumTextBytes,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var before = ConflictResolutionFileStamp.Capture(path);
            if (!before.Exists) return ConflictWorktreeSnapshot.Missing;

            try
            {
                if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
                    throw new InvalidOperationException("Symbolic-link conflicts are not supported yet.");
                var snapshot = await ReadAttemptAsync(path, before, maximumTextBytes, cancellationToken)
                    .ConfigureAwait(false);
                if (snapshot is { } stable) return stable;
            }
            catch (Exception exception) when (
                exception is FileNotFoundException or DirectoryNotFoundException or EndOfStreamException)
            {
                if (attempt + 1 == MaximumAttempts) break;
            }
        }

        throw new InvalidOperationException("The conflict result changed while it was being opened. Please retry.");
    }

    private static async Task<ConflictWorktreeSnapshot?> ReadAttemptAsync(
        string path,
        ConflictResolutionFileStamp before,
        int maximumTextBytes,
        CancellationToken cancellationToken)
    {
        await using var stream = File.Open(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        if (before.Length > maximumTextBytes)
        {
            var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            return CreateIfStable(path, before, Convert.ToHexString(hash), null, isTooLarge: true);
        }

        var bytes = new byte[checked((int)before.Length)];
        await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
        var fingerprint = Convert.ToHexString(SHA256.HashData(bytes));
        return CreateIfStable(path, before, fingerprint, bytes, isTooLarge: false);
    }

    private static ConflictWorktreeSnapshot? CreateIfStable(
        string path,
        ConflictResolutionFileStamp before,
        string fingerprint,
        byte[]? bytes,
        bool isTooLarge)
    {
        var after = ConflictResolutionFileStamp.Capture(path);
        return before == after
            ? new ConflictWorktreeSnapshot(fingerprint, before, bytes, isTooLarge)
            : null;
    }
}

internal readonly record struct ConflictWorktreeSnapshot(
    string Fingerprint,
    ConflictResolutionFileStamp Stamp,
    byte[]? Bytes,
    bool IsTooLarge)
{
    public static readonly ConflictWorktreeSnapshot Missing = new("missing", default, null, false);
}
