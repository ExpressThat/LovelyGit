using System.Text;

namespace LovelyGit.DiffBenchmarks;

internal ref partial struct DirectDiffJsonUtf8SpanWriter
{
    private void Raw(char value) { remaining[0] = (byte)value; Advance(1); }

    private void Raw(string value)
    {
        foreach (var ch in value)
        {
            remaining[0] = (byte)ch;
            Advance(1);
        }
    }

    private void RawUtf8(scoped ReadOnlySpan<char> value)
    {
        if (assumeAscii)
        {
            RawAscii(value);
            return;
        }

        if (TryRawAscii(value))
        {
            return;
        }

        var written = Encoding.UTF8.GetBytes(value, remaining);
        Advance(written);
    }

    private void RawAscii(scoped ReadOnlySpan<char> value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            remaining[index] = (byte)value[index];
        }

        Advance(value.Length);
    }

    private bool TryRawAscii(scoped ReadOnlySpan<char> value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var ch = value[index];
            if (ch > 0x7f)
            {
                return false;
            }

            remaining[index] = (byte)ch;
        }

        Advance(value.Length);
        return true;
    }

    private void Advance(int count) => remaining = remaining[count..];
}
