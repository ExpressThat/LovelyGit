using System.Buffers;
using System.Text;

namespace LovelyGit.DiffBenchmarks;

internal static class DiffJsonStringWriter
{
    private static readonly SearchValues<char> EscapeCharacters =
        SearchValues.Create(DiffJsonEscapeCharacters.Values);

    public static string Write(CommitFileDiffResponse response)
    {
        var builder = new StringBuilder(EstimateCapacity(response));
        builder.Append('{');
        Property(builder, "commitHash", response.CommitHash).Append(',');
        Property(builder, "path", response.Path).Append(',');
        Property(builder, "status", response.Status).Append(',');
        Property(builder, "viewMode", response.ViewMode.ToString()).Append(',');
        builder.Append("\"isBinary\":false,");
        builder.Append("\"hasDifferences\":").Append(response.HasDifferences ? "true" : "false").Append(',');
        builder.Append("\"isTruncated\":false,");
        Property(builder, "truncationMessage", string.Empty).Append(',');
        builder.Append("\"lines\":[");
        DiffJsonLineWriter.Write(builder, response.Plan!, response.ViewMode);
        builder.Append("]}");
        return builder.ToString();
    }

    internal static void RowStart(StringBuilder builder)
    {
        if (builder[^1] != '[')
        {
            builder.Append(',');
        }

        builder.Append('{');
    }

    internal static void NumberProperty(StringBuilder builder, string name, int? value)
    {
        if (!value.HasValue)
        {
            return;
        }

        PrefixProperty(builder);
        builder.Append('"').Append(name).Append("\":").Append(value.Value);
    }

    internal static void TextProperty(StringBuilder builder, string name, ReadOnlySpan<char> value)
    {
        PrefixProperty(builder);
        builder.Append('"').Append(name).Append("\":");
        Escaped(builder, value);
    }

    internal static void ChangeType(StringBuilder builder, string value)
    {
        PrefixProperty(builder);
        builder.Append("\"changeType\":\"").Append(value).Append('"');
    }

    internal static void UnchangedRow(
        StringBuilder builder,
        int oldNumber,
        int newNumber,
        ReadOnlySpan<char> text)
    {
        RowStart(builder);
        builder.Append("\"oldLineNumber\":").Append(oldNumber);
        builder.Append(",\"newLineNumber\":").Append(newNumber);
        builder.Append(",\"text\":");
        Escaped(builder, text);
        builder.Append(",\"changeType\":\"Unchanged\"}");
    }

    private static StringBuilder Property(StringBuilder builder, string name, string value)
    {
        builder.Append('"').Append(name).Append("\":");
        Escaped(builder, value);
        return builder;
    }

    private static void PrefixProperty(StringBuilder builder)
    {
        if (builder[^1] != '{')
        {
            builder.Append(',');
        }
    }

    private static void Escaped(StringBuilder builder, ReadOnlySpan<char> value)
    {
        builder.Append('"');
        var start = 0;
        while (start < value.Length)
        {
            var offset = value[start..].IndexOfAny(EscapeCharacters);
            if (offset < 0)
            {
                builder.Append(value[start..]);
                break;
            }

            var index = start + offset;
            builder.Append(value[start..index]);
            AppendEscaped(builder, value[index]);
            start = index + 1;
        }

        builder.Append('"');
    }

    private static void AppendEscaped(StringBuilder builder, char ch)
    {
        switch (ch)
        {
            case '"':
                builder.Append("\\\"");
                break;
            case '\\':
                builder.Append("\\\\");
                break;
            case '\b':
                builder.Append("\\b");
                break;
            case '\f':
                builder.Append("\\f");
                break;
            case '\n':
                builder.Append("\\n");
                break;
            case '\r':
                builder.Append("\\r");
                break;
            case '\t':
                builder.Append("\\t");
                break;
            default:
                if (ch < ' ')
                {
                    builder.Append("\\u").Append(((int)ch).ToString("x4"));
                }
                else
                {
                    builder.Append(ch);
                }

                break;
        }
    }

    private static int EstimateCapacity(CommitFileDiffResponse response)
    {
        var plan = response.Plan!;
        var textLength = Math.Max(plan.OldText.Length, plan.NewText.Length);
        var rowOverhead = (long)(response.PlannedRows ?? 0) * 96;
        var estimate = textLength + Math.Max(4096L, rowOverhead);
        return (int)Math.Min(int.MaxValue, estimate);
    }

}
