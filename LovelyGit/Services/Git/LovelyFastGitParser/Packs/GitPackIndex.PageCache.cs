using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

internal sealed partial class GitPackIndex
{
    private const int IndexPageBytes = 64 * 1024;
    private const int IndexPageCacheBytes = 8 * 1024 * 1024;
    private static readonly LruCache<IndexPageKey, byte[]> IndexPages = new(
        IndexPageCacheBytes / IndexPageBytes,
        IndexPageCacheBytes,
        static page => page.LongLength);
    private static long _nextCacheGeneration;
    private readonly long _cacheGeneration = Interlocked.Increment(ref _nextCacheGeneration);

    internal static long CachedIndexBytes => IndexPages.CurrentWeight;

    private void ReadExactlyAt(Span<byte> destination, long offset)
    {
        while (!destination.IsEmpty)
        {
            var pageIndex = offset / IndexPageBytes;
            var pageOffset = checked((int)(offset % IndexPageBytes));
            var page = GetIndexPage(pageIndex);
            var available = Math.Min(destination.Length, page.Length - pageOffset);
            if (available <= 0)
            {
                throw new EndOfStreamException();
            }

            page.AsSpan(pageOffset, available).CopyTo(destination);
            destination = destination[available..];
            offset += available;
        }
    }

    private byte[] GetIndexPage(long pageIndex)
    {
        var key = new IndexPageKey(_cacheGeneration, pageIndex);
        if (IndexPages.TryGet(key, out var page))
        {
            return page;
        }

        var offset = pageIndex * IndexPageBytes;
        var remaining = _file.Length - offset;
        if (remaining <= 0)
        {
            throw new EndOfStreamException();
        }

        page = new byte[Math.Min(IndexPageBytes, checked((int)remaining))];
        var filled = 0;
        while (filled < page.Length)
        {
            var read = RandomAccess.Read(
                _file.SafeFileHandle,
                page.AsSpan(filled),
                offset + filled);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            filled += read;
        }

        IndexPages.Set(key, page);
        return page;
    }

    private readonly record struct IndexPageKey(long Generation, long PageIndex);
}
