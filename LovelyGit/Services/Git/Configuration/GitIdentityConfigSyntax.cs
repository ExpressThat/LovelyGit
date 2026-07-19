using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.Configuration;

internal enum GitIdentityConfigSection
{
    Other,
    User,
    Include,
    IncludeIf,
    Extensions,
}

internal static class GitIdentityConfigSyntax
{
    public static bool TryReadSection(
        ReadOnlySpan<char> line,
        out GitIdentityConfigSection section,
        out string? subsection)
    {
        section = GitIdentityConfigSection.Other;
        subsection = null;
        if (line.Length < 3 || line[0] != '[' || line[^1] != ']') return false;

        var content = line[1..^1].Trim();
        var separator = content.IndexOfAny(' ', '\t');
        var name = separator < 0 ? content : content[..separator];
        section = ParseSection(name);
        if (section == GitIdentityConfigSection.IncludeIf && separator >= 0)
        {
            subsection = Unquote(content[(separator + 1)..].Trim());
        }

        return true;
    }

    public static bool TryReadValue(
        ReadOnlySpan<char> line,
        out ReadOnlySpan<char> key,
        out ReadOnlySpan<char> value)
    {
        key = default;
        value = default;
        var separator = line.IndexOf('=');
        if (separator <= 0) return false;
        key = line[..separator].Trim();
        value = StripComment(line[(separator + 1)..].Trim());
        return !key.IsEmpty;
    }

    public static bool IsEnabled(ReadOnlySpan<char> value)
    {
        value = TrimQuotes(value);
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("on", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.Ordinal);
    }

    public static string Unquote(ReadOnlySpan<char> value)
    {
        if (value.Length < 2 || value[0] != '"' || value[^1] != '"')
        {
            return value.ToString();
        }

        var builder = new StringBuilder(value.Length - 2);
        var escaped = false;
        foreach (var character in value[1..^1])
        {
            if (!escaped && character == '\\')
            {
                escaped = true;
                continue;
            }

            builder.Append(escaped ? Unescape(character) : character);
            escaped = false;
        }

        if (escaped) builder.Append('\\');
        return builder.ToString();
    }

    private static GitIdentityConfigSection ParseSection(ReadOnlySpan<char> name)
    {
        if (name.Equals("user", StringComparison.OrdinalIgnoreCase))
            return GitIdentityConfigSection.User;
        if (name.Equals("include", StringComparison.OrdinalIgnoreCase))
            return GitIdentityConfigSection.Include;
        if (name.Equals("includeif", StringComparison.OrdinalIgnoreCase))
            return GitIdentityConfigSection.IncludeIf;
        if (name.Equals("extensions", StringComparison.OrdinalIgnoreCase))
            return GitIdentityConfigSection.Extensions;
        return GitIdentityConfigSection.Other;
    }

    private static ReadOnlySpan<char> StripComment(ReadOnlySpan<char> value)
    {
        var quoted = false;
        var escaped = false;
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (escaped) escaped = false;
            else if (character == '\\') escaped = true;
            else if (character == '"') quoted = !quoted;
            else if (!quoted && character is '#' or ';' &&
                     (index == 0 || char.IsWhiteSpace(value[index - 1])))
                return value[..index].TrimEnd();
        }

        return value;
    }

    private static ReadOnlySpan<char> TrimQuotes(ReadOnlySpan<char> value) =>
        value.Length >= 2 && value[0] == '"' && value[^1] == '"'
            ? value[1..^1]
            : value;

    private static char Unescape(char value) => value switch
    {
        'n' => '\n',
        't' => '\t',
        'b' => '\b',
        _ => value,
    };
}
