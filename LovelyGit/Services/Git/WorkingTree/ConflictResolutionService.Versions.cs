using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionService
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private static async Task<ConflictFileVersion> ReadVersionAsync(
        GitObjectStore objectStore,
        GitIndexEntry? entry,
        CancellationToken cancellationToken)
    {
        if (entry == null) return new ConflictFileVersion();
        if (entry.FileSize > MaxTextBytes)
        {
            return new ConflictFileVersion
            {
                Exists = true,
                IsTooLarge = true,
                SizeBytes = entry.FileSize,
            };
        }

        var data = await objectStore
            .ReadObjectWithoutCachingAsync(entry.ObjectId, cancellationToken)
            .ConfigureAwait(false);
        if (data.Kind != GitObjectKind.Blob)
        {
            throw new InvalidDataException($"Object is not a blob: {entry.ObjectId}");
        }
        return CreateVersion(data.Data);
    }

    private static ConflictFileVersion CreateWorktreeVersion(ConflictWorktreeSnapshot snapshot)
    {
        if (!snapshot.Stamp.Exists) return new ConflictFileVersion();
        if (snapshot.IsTooLarge)
        {
            return new ConflictFileVersion
            {
                Exists = true,
                IsTooLarge = true,
                SizeBytes = snapshot.Stamp.Length,
            };
        }

        return CreateVersion(snapshot.Bytes!);
    }

    private static ConflictFileVersion CreateVersion(byte[] bytes)
    {
        var isBinary = WorkingTreeChangeService.IsBinary(bytes);
        string? text = null;
        if (!isBinary)
        {
            try
            {
                text = StrictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                isBinary = true;
            }
        }

        return new ConflictFileVersion
        {
            Exists = true,
            IsBinary = isBinary,
            SizeBytes = bytes.Length,
            Text = text,
        };
    }
}
