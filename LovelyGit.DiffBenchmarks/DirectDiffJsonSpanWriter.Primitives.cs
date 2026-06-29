using System.Buffers;

namespace LovelyGit.DiffBenchmarks;

internal ref partial struct DirectDiffJsonSpanWriter
{
    private static readonly SearchValues<char> EscapeCharacters =
        SearchValues.Create(DiffJsonEscapeCharacters.Values);

    private void Row(int? oldNumber, int? newNumber, scoped ReadOnlySpan<char> oldText, bool hasOld, scoped ReadOnlySpan<char> newText, bool hasNew, string changeType, CommitDiffViewMode viewMode)
    {
        Raw('{');
        Number("oldLineNumber", oldNumber, hasPrevious: false);
        Number("newLineNumber", newNumber, oldNumber.HasValue);
        if (viewMode == CommitDiffViewMode.SideBySide)
        {
            SideBySideText(oldText, hasOld, newText, hasNew);
        }
        else
        {
            Text("text", hasNew ? newText : oldText, oldNumber.HasValue || newNumber.HasValue);
        }

        Property("changeType", changeType, hasPrevious: true);
        Raw('}');
    }

    private void Unchanged(int oldNumber, int newNumber, scoped ReadOnlySpan<char> line)
    {
        Raw('{');
        Raw("\"oldLineNumber\":");
        NumberValue(oldNumber);
        Raw(",\"newLineNumber\":");
        NumberValue(newNumber);
        Raw(",\"text\":");
        Escaped(line);
        Raw(",\"changeType\":\"Unchanged\"}");
    }

    private void SideBySideText(scoped ReadOnlySpan<char> oldText, bool hasOld, scoped ReadOnlySpan<char> newText, bool hasNew)
    {
        if (!hasOld && !hasNew)
        {
            Text("text", newText, hasPrevious: true);
            return;
        }

        if (hasOld)
        {
            Text("oldText", oldText, hasPrevious: true);
        }

        if (hasNew)
        {
            Text("newText", newText, hasOld);
        }
    }

    private void RowSeparator(bool hasRows)
    {
        if (hasRows)
        {
            Raw(',');
        }
    }

    private void Number(string name, int? value, bool hasPrevious)
    {
        if (!value.HasValue)
        {
            return;
        }

        Prefix(hasPrevious);
        Raw('"');
        Raw(name);
        Raw("\":");
        NumberValue(value.Value);
    }

    private void Property(string name, string value, bool hasPrevious = false) => Text(name, value, hasPrevious);

    private void Text(string name, scoped ReadOnlySpan<char> value, bool hasPrevious)
    {
        Prefix(hasPrevious);
        Raw('"');
        Raw(name);
        Raw("\":");
        Escaped(value);
    }

    private void Prefix(bool hasPrevious)
    {
        if (hasPrevious)
        {
            Raw(',');
        }
    }

    private void Escaped(scoped ReadOnlySpan<char> value)
    {
        Raw('"');
        var start = 0;
        while (start < value.Length)
        {
            var offset = value[start..].IndexOfAny(EscapeCharacters);
            if (offset < 0)
            {
                Raw(value[start..]);
                break;
            }

            var index = start + offset;
            Raw(value[start..index]);
            EscapedChar(value[index]);
            start = index + 1;
        }

        Raw('"');
    }

    private void EscapedChar(char ch)
    {
        switch (ch)
        {
            case '"':
                Raw("\\\"");
                break;
            case '\\':
                Raw("\\\\");
                break;
            case '\b':
                Raw("\\b");
                break;
            case '\f':
                Raw("\\f");
                break;
            case '\n':
                Raw("\\n");
                break;
            case '\r':
                Raw("\\r");
                break;
            case '\t':
                Raw("\\t");
                break;
            default:
                if (ch < ' ')
                {
                    Raw("\\u00");
                    Raw(Hex(ch >> 4));
                    Raw(Hex(ch));
                }
                else
                {
                    Raw(ch);
                }

                break;
        }
    }

    private void NumberValue(int value)
    {
        var written = DigitCount(value);
        var target = remaining[..written];
        for (var index = written - 1; index >= 0; index--)
        {
            target[index] = (char)('0' + value % 10);
            value /= 10;
        }

        Advance(written);
    }

    private static int DigitCount(int value) =>
        value < 10 ? 1 :
        value < 100 ? 2 :
        value < 1000 ? 3 :
        value < 10000 ? 4 :
        value < 100000 ? 5 :
        value < 1000000 ? 6 :
        value < 10000000 ? 7 :
        value < 100000000 ? 8 :
        value < 1000000000 ? 9 : 10;

    private void Raw(char value) { remaining[0] = value; Advance(1); }
    private void Raw(scoped ReadOnlySpan<char> value) { value.CopyTo(remaining); Advance(value.Length); }
    private void Raw(string value) { value.AsSpan().CopyTo(remaining); Advance(value.Length); }
    private void Advance(int count) => remaining = remaining[count..];
    private static char Hex(int value) => (char)(value < 10 ? '0' + value : 'a' + value - 10);
}
