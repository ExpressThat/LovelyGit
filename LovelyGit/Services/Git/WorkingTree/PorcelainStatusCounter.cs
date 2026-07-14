namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class PorcelainStatusCounter : Stream
{
    private int _recordOffset;
    private bool _hasSecondPath;

    public int Count { get; private set; }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count) =>
        CountRecords(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer) => CountRecords(buffer);

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CountRecords(buffer.Span);
        return ValueTask.CompletedTask;
    }

    private void CountRecords(ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            if (_recordOffset < 2)
            {
                _hasSecondPath |= value is (byte)'R' or (byte)'C';
                _recordOffset++;
                continue;
            }

            if (value != 0)
            {
                _recordOffset++;
                continue;
            }

            if (_hasSecondPath && _recordOffset >= 3)
            {
                _hasSecondPath = false;
                _recordOffset = 2;
                continue;
            }

            Count++;
            _recordOffset = 0;
        }
    }

    public override void Flush()
    {
    }

    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
