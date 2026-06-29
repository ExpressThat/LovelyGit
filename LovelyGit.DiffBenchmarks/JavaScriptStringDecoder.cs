using System.Globalization;

namespace LovelyGit.DiffBenchmarks;

internal static class JavaScriptStringDecoder
{
    public static string Decode(string value)
    {
        if (!value.Contains('\\', StringComparison.Ordinal))
        {
            return value;
        }

        var builder = new System.Text.StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            var ch = value[index];
            if (ch != '\\' || index + 1 >= value.Length)
            {
                builder.Append(ch);
                continue;
            }

            index++;
            AppendEscaped(builder, value, ref index);
        }

        return builder.ToString();
    }

    private static void AppendEscaped(
        System.Text.StringBuilder builder,
        string value,
        ref int index)
    {
        switch (value[index])
        {
            case '"':
            case '\\':
            case '/':
                builder.Append(value[index]);
                break;
            case 'b':
                builder.Append('\b');
                break;
            case 'f':
                builder.Append('\f');
                break;
            case 'n':
                builder.Append('\n');
                break;
            case 'r':
                builder.Append('\r');
                break;
            case 't':
                builder.Append('\t');
                break;
            case 'u' when index + 4 < value.Length:
                builder.Append((char)int.Parse(value.AsSpan(index + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                index += 4;
                break;
            default:
                builder.Append(value[index]);
                break;
        }
    }
}
