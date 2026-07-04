using Microsoft.Win32.SafeHandles;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

internal sealed class RandomAccessPackStream : Stream
{
    private readonly SafeFileHandle _handle;
    private long _position;

    public RandomAccessPackStream(SafeFileHandle handle, long position)
    {
        _handle = handle;
        _position = position;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = Read(buffer.AsSpan(offset, count));
        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        var read = RandomAccess.Read(_handle, buffer, _position);
        _position += read;
        return read;
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        var read = await RandomAccess.ReadAsync(_handle, buffer, _position, cancellationToken)
            .ConfigureAwait(false);
        _position += read;
        return read;
    }

    public override int ReadByte()
    {
        Span<byte> buffer = stackalloc byte[1];
        return Read(buffer) == 0 ? -1 : buffer[0];
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
