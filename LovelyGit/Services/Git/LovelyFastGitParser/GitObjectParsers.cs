using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal static class GitObjectParsers
{
    public static GitCommit ParseCommit(GitObjectId id, byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        var separator = text.IndexOf("\n\n", StringComparison.Ordinal);
        var headerText = separator >= 0 ? text.AsSpan(0, separator) : text.AsSpan();
        var body = separator >= 0 ? text[(separator + 2)..] : string.Empty;
        var commit = new GitCommit { Hash = id, Body = body };

        foreach (var rawLine in EnumerateLines(headerText))
        {
            var line = TrimTrailingCarriageReturn(rawLine);
            if (line.StartsWith("parent ", StringComparison.Ordinal))
            {
                commit.ParentHashes.Add(GitObjectId.Parse(line["parent ".Length..].Trim(), id.ObjectFormat));
            }
            else if (line.StartsWith("tree ", StringComparison.Ordinal))
            {
                commit.TreeHash = GitObjectId.Parse(line["tree ".Length..].Trim(), id.ObjectFormat);
            }
            else if (line.StartsWith("author ", StringComparison.Ordinal))
            {
                var author = ParseSignature(line["author ".Length..].Trim());
                commit.AuthorName = author.Name;
                commit.AuthorEmail = author.Email;
                commit.AuthorUnixSeconds = author.UnixSeconds;
            }
        }

        var trimmedBody = body.Trim('\n', '\r');
        var newline = trimmedBody.IndexOf('\n');
        commit.Subject = newline >= 0 ? trimmedBody[..newline].TrimEnd('\r') : trimmedBody;
        return commit;
    }

    public static GitTag ParseTag(GitObjectId id, GitObjectFormat objectFormat, byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        var tagText = text.AsSpan();
        GitObjectId? target = null;
        var targetType = string.Empty;
        var name = string.Empty;

        foreach (var rawLine in EnumerateLines(tagText))
        {
            var line = TrimTrailingCarriageReturn(rawLine);
            if (line.Length == 0)
            {
                break;
            }

            if (line.StartsWith("object ", StringComparison.Ordinal))
            {
                target = GitObjectId.Parse(line["object ".Length..].Trim(), objectFormat);
            }
            else if (line.StartsWith("type ", StringComparison.Ordinal))
            {
                targetType = line["type ".Length..].Trim().ToString();
            }
            else if (line.StartsWith("tag ", StringComparison.Ordinal))
            {
                name = line["tag ".Length..].Trim().ToString();
            }
        }

        if (target == null)
        {
            throw new InvalidDataException($"Tag object has no target: {id}");
        }

        return new GitTag(id, target.Value, name, targetType);
    }

    public static List<GitTreeEntry> ParseTreeEntries(
        GitObjectId treeId,
        GitObjectFormat objectFormat,
        GitObjectData data,
        string prefix)
    {
        if (data.Kind != GitObjectKind.Tree)
        {
            throw new InvalidDataException($"Object is not a tree: {treeId}");
        }

        var entries = new List<GitTreeEntry>();
        var index = 0;
        while (index < data.Data.Length)
        {
            var modeStart = index;
            while (index < data.Data.Length && data.Data[index] != (byte)' ')
            {
                index++;
            }

            if (index >= data.Data.Length)
            {
                throw new InvalidDataException("Tree entry has no mode terminator.");
            }

            var mode = Encoding.ASCII.GetString(data.Data, modeStart, index - modeStart);
            index++;

            var nameStart = index;
            while (index < data.Data.Length && data.Data[index] != 0)
            {
                index++;
            }

            if (index >= data.Data.Length)
            {
                throw new InvalidDataException("Tree entry has no name terminator.");
            }

            var name = Encoding.UTF8.GetString(data.Data, nameStart, index - nameStart);
            index++;

            var hashLength = GitObjectId.GetByteLength(objectFormat);
            if (index + hashLength > data.Data.Length)
            {
                throw new InvalidDataException("Tree entry object id is truncated.");
            }

            var objectId = new GitObjectId(
                Convert.ToHexString(data.Data.AsSpan(index, hashLength)).ToLowerInvariant(),
                objectFormat);
            index += hashLength;

            var path = string.IsNullOrEmpty(prefix) ? name : $"{prefix}/{name}";
            entries.Add(new GitTreeEntry(name, path, objectId, mode));
        }

        return entries;
    }

    private static (string Name, string Email, long UnixSeconds) ParseSignature(ReadOnlySpan<char> value)
    {
        var emailStart = value.LastIndexOf('<');
        var emailEnd = value.LastIndexOf('>');
        if (emailStart < 0 || emailEnd <= emailStart)
        {
            return (value.ToString(), string.Empty, 0);
        }

        var name = value[..emailStart].Trim().ToString();
        var email = value[(emailStart + 1)..emailEnd].Trim().ToString();
        var rest = value[(emailEnd + 1)..].Trim();
        var firstSpace = rest.IndexOf(' ');
        var secondsText = firstSpace >= 0 ? rest[..firstSpace] : rest;
        return long.TryParse(secondsText, out var seconds)
            ? (name, email, seconds)
            : (name, email, 0);
    }

    private static LineEnumerator EnumerateLines(ReadOnlySpan<char> text)
    {
        return new LineEnumerator(text);
    }

    private static ReadOnlySpan<char> TrimTrailingCarriageReturn(ReadOnlySpan<char> value)
    {
        return value.Length > 0 && value[^1] == '\r' ? value[..^1] : value;
    }

    private ref struct LineEnumerator
    {
        private ReadOnlySpan<char> _remaining;

        public LineEnumerator(ReadOnlySpan<char> text)
        {
            Current = default;
            _remaining = text;
        }

        public ReadOnlySpan<char> Current { get; private set; }

        public LineEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_remaining.Length == 0)
            {
                return false;
            }

            var newlineIndex = _remaining.IndexOf('\n');
            if (newlineIndex < 0)
            {
                Current = _remaining;
                _remaining = default;
                return true;
            }

            Current = _remaining[..newlineIndex];
            _remaining = _remaining[(newlineIndex + 1)..];
            return true;
        }
    }
}
