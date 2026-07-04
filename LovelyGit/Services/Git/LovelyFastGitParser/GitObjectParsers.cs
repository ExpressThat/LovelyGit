using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal static partial class GitObjectParsers
{
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

            var objectId = GitObjectId.FromBytes(data.Data.AsSpan(index, hashLength), objectFormat);
            index += hashLength;

            var path = string.IsNullOrEmpty(prefix) ? name : $"{prefix}/{name}";
            entries.Add(new GitTreeEntry(name, path, objectId, mode));
        }

        return entries;
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
