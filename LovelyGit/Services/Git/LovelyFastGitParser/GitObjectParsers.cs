using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal static class GitObjectParsers
{
    public static GitCommit ParseCommit(GitObjectId id, byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        var separator = text.IndexOf("\n\n", StringComparison.Ordinal);
        var headerText = separator >= 0 ? text[..separator] : text;
        var body = separator >= 0 ? text[(separator + 2)..] : string.Empty;
        var commit = new GitCommit { Hash = id, Body = body };

        foreach (var line in headerText.Split('\n'))
        {
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
        GitObjectId? target = null;
        var targetType = string.Empty;
        var name = string.Empty;

        foreach (var line in text.Split('\n'))
        {
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
                targetType = line["type ".Length..].Trim();
            }
            else if (line.StartsWith("tag ", StringComparison.Ordinal))
            {
                name = line["tag ".Length..].Trim();
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

    private static (string Name, string Email, long UnixSeconds) ParseSignature(string value)
    {
        var emailStart = value.LastIndexOf('<');
        var emailEnd = value.LastIndexOf('>');
        if (emailStart < 0 || emailEnd <= emailStart)
        {
            return (value, string.Empty, 0);
        }

        var name = value[..emailStart].Trim();
        var email = value[(emailStart + 1)..emailEnd].Trim();
        var rest = value[(emailEnd + 1)..].Trim();
        var firstSpace = rest.IndexOf(' ');
        var secondsText = firstSpace >= 0 ? rest[..firstSpace] : rest;
        return long.TryParse(secondsText, out var seconds)
            ? (name, email, seconds)
            : (name, email, 0);
    }
}
