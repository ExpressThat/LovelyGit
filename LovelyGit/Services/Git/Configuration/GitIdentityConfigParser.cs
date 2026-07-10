using System.Text;
using ExpressThat.LovelyGit.Services.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Git.Configuration;

internal sealed class GitIdentityConfigParser
{
    private const int MaximumIncludeDepth = 16;
    private readonly string _gitDirectory;
    private readonly string? _branchName;
    private readonly string? _homeDirectory;
    private readonly HashSet<string> _activeIncludes = new(StringComparer.OrdinalIgnoreCase);

    public GitIdentityConfigParser(
        string gitDirectory,
        string? branchName,
        string? homeDirectory)
    {
        _gitDirectory = gitDirectory;
        _branchName = branchName;
        _homeDirectory = homeDirectory;
    }

    public Task ReadAsync(
        string path,
        GitIdentityValueSource source,
        GitIdentityAccumulator identity,
        CancellationToken cancellationToken) =>
        ReadAsync(path, source, identity, 0, cancellationToken);

    private async Task ReadAsync(
        string path,
        GitIdentityValueSource source,
        GitIdentityAccumulator identity,
        int depth,
        CancellationToken cancellationToken)
    {
        if (depth >= MaximumIncludeDepth || !File.Exists(path))
        {
            return;
        }

        var fullPath = Path.GetFullPath(path);
        if (!_activeIncludes.Add(fullPath))
        {
            return;
        }

        try
        {
            using var trace = LovelyGitTrace.Time(
                "git.identity.config", $"{source} {Path.GetFileName(fullPath)}");
            using var stream = new FileStream(
                fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            var section = string.Empty;
            string? subsection = null;
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } rawLine)
            {
                var line = rawLine.AsSpan().Trim();
                if (line.Length == 0 || line[0] is '#' or ';')
                {
                    continue;
                }

                if (TryReadSection(line, out var nextSection, out var nextSubsection))
                {
                    section = nextSection;
                    subsection = nextSubsection;
                    continue;
                }

                if (!TryReadValue(line, out var key, out var value))
                {
                    continue;
                }

                if (section.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyIdentity(key, value, source, identity);
                }
                else if (section.Equals("include", StringComparison.OrdinalIgnoreCase) &&
                         key.Equals("path", StringComparison.OrdinalIgnoreCase))
                {
                    await ReadIncludeAsync(value, fullPath, source, identity, depth, cancellationToken)
                        .ConfigureAwait(false);
                }
                else if (section.Equals("includeif", StringComparison.OrdinalIgnoreCase) &&
                         subsection is not null && key.Equals("path", StringComparison.OrdinalIgnoreCase) &&
                         GitConfigConditionMatcher.Matches(
                             subsection, _gitDirectory, _branchName, _homeDirectory, fullPath))
                {
                    await ReadIncludeAsync(value, fullPath, source, identity, depth, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _activeIncludes.Remove(fullPath);
        }
    }

    private Task ReadIncludeAsync(
        string value,
        string includingPath,
        GitIdentityValueSource source,
        GitIdentityAccumulator identity,
        int depth,
        CancellationToken cancellationToken)
    {
        var includePath = GitConfigConditionMatcher.ResolveIncludePath(
            value, _homeDirectory, includingPath);
        return ReadAsync(includePath, source, identity, depth + 1, cancellationToken);
    }

    private static void ApplyIdentity(
        string key,
        string value,
        GitIdentityValueSource source,
        GitIdentityAccumulator identity)
    {
        if (key.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            identity.ApplyName(value, source);
        }
        else if (key.Equals("email", StringComparison.OrdinalIgnoreCase))
        {
            identity.ApplyEmail(value, source);
        }
    }

    private static bool TryReadSection(
        ReadOnlySpan<char> line,
        out string section,
        out string? subsection)
    {
        section = string.Empty;
        subsection = null;
        if (line.Length < 3 || line[0] != '[' || line[^1] != ']')
        {
            return false;
        }

        var content = line[1..^1].Trim();
        var separator = content.IndexOfAny(' ', '\t');
        if (separator < 0)
        {
            section = content.ToString();
            return true;
        }

        section = content[..separator].ToString();
        subsection = Unquote(content[(separator + 1)..].Trim());
        return true;
    }

    private static bool TryReadValue(
        ReadOnlySpan<char> line,
        out string key,
        out string value)
    {
        key = string.Empty;
        value = string.Empty;
        var separator = line.IndexOf('=');
        if (separator <= 0)
        {
            return false;
        }

        key = line[..separator].Trim().ToString();
        value = Unquote(StripComment(line[(separator + 1)..].Trim()));
        return key.Length > 0;
    }

    private static ReadOnlySpan<char> StripComment(ReadOnlySpan<char> value)
    {
        var quoted = false;
        var escaped = false;
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (character == '\\')
            {
                escaped = true;
            }
            else if (character == '"')
            {
                quoted = !quoted;
            }
            else if (!quoted && character is '#' or ';' &&
                     (index == 0 || char.IsWhiteSpace(value[index - 1])))
            {
                return value[..index].TrimEnd();
            }
        }

        return value;
    }

    private static string Unquote(ReadOnlySpan<char> value)
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

        if (escaped)
        {
            builder.Append('\\');
        }

        return builder.ToString();
    }

    private static char Unescape(char value) => value switch
    {
        'n' => '\n',
        't' => '\t',
        'b' => '\b',
        _ => value,
    };
}
