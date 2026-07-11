using System.Buffers;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class ConflictToolSnapshot : IDisposable
{
    private readonly string _directory;
    private readonly bool _targetExisted;
    private readonly bool _indexExisted;

    private ConflictToolSnapshot(string directory, bool targetExisted, bool indexExisted)
    {
        _directory = directory;
        _targetExisted = targetExisted;
        _indexExisted = indexExisted;
    }

    public static async Task<ConflictToolSnapshot> CreateAsync(
        string targetPath,
        string indexPath,
        CancellationToken cancellationToken)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"lovelygit-merge-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var snapshot = new ConflictToolSnapshot(directory, File.Exists(targetPath), File.Exists(indexPath));
        try
        {
            await CopyIfPresentAsync(targetPath, snapshot.TargetPath, cancellationToken).ConfigureAwait(false);
            await CopyIfPresentAsync(indexPath, snapshot.IndexPath, cancellationToken).ConfigureAwait(false);
            return snapshot;
        }
        catch
        {
            snapshot.Dispose();
            throw;
        }
    }

    public async Task RestoreAsync(string targetPath, string indexPath)
    {
        await RestoreFileAsync(TargetPath, targetPath, _targetExisted).ConfigureAwait(false);
        await RestoreFileAsync(IndexPath, indexPath, _indexExisted).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private string TargetPath => Path.Combine(_directory, "target");
    private string IndexPath => Path.Combine(_directory, "index");

    private static async Task CopyIfPresentAsync(
        string source,
        string destination,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(source)) return;
        await using var input = File.Open(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        await using var output = File.Create(destination);
        await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
    }

    private static async Task RestoreFileAsync(string snapshot, string destination, bool existed)
    {
        if (!existed)
        {
            if (File.Exists(destination)) File.Delete(destination);
            return;
        }

        if (File.Exists(destination) && await FilesEqualAsync(snapshot, destination).ConfigureAwait(false))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await using var input = File.OpenRead(snapshot);
        await using var output = File.Open(destination, FileMode.Create, FileAccess.Write, FileShare.None);
        await input.CopyToAsync(output).ConfigureAwait(false);
    }

    private static async Task<bool> FilesEqualAsync(string leftPath, string rightPath)
    {
        if (new FileInfo(leftPath).Length != new FileInfo(rightPath).Length) return false;
        await using var left = File.OpenRead(leftPath);
        await using var right = File.Open(rightPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var leftBuffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        var rightBuffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        try
        {
            while (true)
            {
                var leftCount = await left.ReadAsync(leftBuffer).ConfigureAwait(false);
                var rightCount = await right.ReadAsync(rightBuffer).ConfigureAwait(false);
                if (leftCount != rightCount) return false;
                if (leftCount == 0) return true;
                if (!leftBuffer.AsSpan(0, leftCount).SequenceEqual(rightBuffer.AsSpan(0, rightCount)))
                {
                    return false;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(leftBuffer);
            ArrayPool<byte>.Shared.Return(rightBuffer);
        }
    }
}
