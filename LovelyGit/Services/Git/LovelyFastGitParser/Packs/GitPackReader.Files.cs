using Microsoft.Win32.SafeHandles;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

internal sealed partial class GitPackReader
{
    private readonly object _packFilesGate = new();
    private readonly Dictionary<string, PackFileEntry> _packFiles = new(StringComparer.Ordinal);
    private bool _packFilesDisposed;

    internal int OpenPackFileCount
    {
        get
        {
            lock (_packFilesGate) return _packFiles.Count;
        }
    }

    public void ClearPackFiles()
    {
        ClearObjectCache();
        lock (_packFilesGate)
        {
            RetireAllCore();
        }
    }

    public void RetirePackFile(string packPath)
    {
        lock (_packFilesGate)
        {
            if (_packFiles.Remove(packPath, out var entry))
            {
                RetireCore(entry);
            }
        }
    }

    private void DisposePackFiles()
    {
        ClearObjectCache();
        lock (_packFilesGate)
        {
            _packFilesDisposed = true;
            RetireAllCore();
        }
    }

    private void RetireAllCore()
    {
        foreach (var entry in _packFiles.Values)
        {
            RetireCore(entry);
        }

        _packFiles.Clear();
    }

    private PackFileLease AcquirePackFile(string packPath)
    {
        lock (_packFilesGate)
        {
            ObjectDisposedException.ThrowIf(_packFilesDisposed, this);
            if (!_packFiles.TryGetValue(packPath, out var entry))
            {
                entry = new PackFileEntry(new FileStream(
                    packPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 1,
                    FileOptions.RandomAccess));
                _packFiles.Add(packPath, entry);
            }

            entry.ActiveReaders++;
            return new PackFileLease(this, entry);
        }
    }

    private void Release(PackFileEntry entry)
    {
        lock (_packFilesGate)
        {
            entry.ActiveReaders--;
            if (entry.Retired && entry.ActiveReaders == 0)
            {
                DisposeCore(entry);
            }
        }
    }

    private static void RetireCore(PackFileEntry entry)
    {
        entry.Retired = true;
        if (entry.ActiveReaders == 0)
        {
            DisposeCore(entry);
        }
    }

    private static void DisposeCore(PackFileEntry entry)
    {
        if (entry.Disposed) return;
        entry.Disposed = true;
        entry.File.Dispose();
    }

    private sealed class PackFileEntry(FileStream file)
    {
        public FileStream File { get; } = file;
        public int ActiveReaders { get; set; }
        public bool Disposed { get; set; }
        public bool Retired { get; set; }
    }

    private readonly struct PackFileLease(GitPackReader owner, PackFileEntry entry) : IDisposable
    {
        public SafeFileHandle Handle => entry.File.SafeFileHandle;

        public void Dispose() => owner.Release(entry);
    }
}
