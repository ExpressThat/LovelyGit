using System.Buffers;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class PooledSequentialReader : IDisposable
{
    private const int DefaultBufferSize = 64 * 1024;
    private readonly Stream _stream;
    private readonly byte[] _buffer;
    private int _offset;
    private int _length;
    private bool _disposed;

    public PooledSequentialReader(Stream stream, int bufferSize = DefaultBufferSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);
        _stream = stream;
        _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
    }

    public long Position { get; private set; }

    public int ReadByte()
    {
        if (_offset >= _length && !Refill())
        {
            return -1;
        }

        Position++;
        return _buffer[_offset++];
    }

    public void ReadExactly(Span<byte> destination)
    {
        while (!destination.IsEmpty)
        {
            if (_offset >= _length && !Refill())
            {
                throw new EndOfStreamException();
            }

            var count = Math.Min(destination.Length, _length - _offset);
            _buffer.AsSpan(_offset, count).CopyTo(destination);
            _offset += count;
            Position += count;
            destination = destination[count..];
        }
    }

    public void Skip(long count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        while (count > 0)
        {
            if (_offset >= _length && !Refill())
            {
                throw new EndOfStreamException();
            }

            var skipped = (int)Math.Min(count, _length - _offset);
            _offset += skipped;
            Position += skipped;
            count -= skipped;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ArrayPool<byte>.Shared.Return(_buffer);
    }

    private bool Refill()
    {
        _offset = 0;
        _length = _stream.Read(_buffer, 0, _buffer.Length);
        return _length > 0;
    }
}
