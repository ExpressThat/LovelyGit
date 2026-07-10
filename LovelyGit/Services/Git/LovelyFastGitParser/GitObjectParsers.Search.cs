using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal static partial class GitObjectParsers
{
    public static GitCommitSearchHeader ParseCommitSearchHeader(
        GitObjectId id,
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> queryUtf8,
        string query)
    {
        var bodyStart = FindBodyStart(data);
        var header = bodyStart >= 0 ? data[..(bodyStart - 2)] : data;
        var body = bodyStart >= 0 ? data[bodyStart..] : ReadOnlySpan<byte>.Empty;
        var firstParent = default(GitObjectId);
        List<GitObjectId>? additionalParents = null;
        var parentCount = 0;
        var authorUnixSeconds = 0L;
        var identityMatches = false;
        foreach (var rawLine in EnumerateByteLines(header))
        {
            var line = TrimTrailingCarriageReturn(rawLine);
            if (line.StartsWith("parent "u8))
            {
                var parent = ParseObjectId(line["parent "u8.Length..], id.ObjectFormat);
                if (parentCount++ == 0)
                {
                    firstParent = parent;
                }
                else
                {
                    (additionalParents ??= new List<GitObjectId>(1)).Add(parent);
                }
            }
            else if (line.StartsWith("author "u8))
            {
                var author = TrimAscii(line["author "u8.Length..]);
                authorUnixSeconds = ParseAuthorTimestamp(author);
                identityMatches = ContainsUtf8IgnoreCase(GetAuthorIdentity(author), queryUtf8);
            }
        }

        var messageMatches = ContainsUtf8IgnoreCase(body, queryUtf8);
        if (!messageMatches && !identityMatches && ContainsNonAscii(queryUtf8))
        {
            messageMatches = Encoding.UTF8.GetString(data)
                .Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        return new GitCommitSearchHeader(
            firstParent,
            additionalParents?.ToArray(),
            parentCount,
            authorUnixSeconds,
            identityMatches || messageMatches);
    }

    private static ReadOnlySpan<byte> GetAuthorIdentity(ReadOnlySpan<byte> author)
    {
        var emailEnd = author.LastIndexOf((byte)'>');
        return emailEnd >= 0 ? author[..(emailEnd + 1)] : author;
    }

    private static long ParseAuthorTimestamp(ReadOnlySpan<byte> author)
    {
        var emailEnd = author.LastIndexOf((byte)'>');
        return emailEnd >= 0
            ? ParseLeadingInt64(TrimAscii(author[(emailEnd + 1)..]))
            : 0;
    }

    private static bool ContainsNonAscii(ReadOnlySpan<byte> value)
    {
        foreach (var current in value)
        {
            if (current >= 0x80)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsUtf8IgnoreCase(
        ReadOnlySpan<byte> source,
        ReadOnlySpan<byte> query)
    {
        if (query.IsEmpty || query.Length > source.Length)
        {
            return false;
        }

        var lastStart = source.Length - query.Length;
        for (var start = 0; start <= lastStart; start++)
        {
            var matched = true;
            for (var offset = 0; offset < query.Length; offset++)
            {
                if (FoldAscii(source[start + offset]) != FoldAscii(query[offset]))
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return true;
            }
        }

        return false;
    }

    private static byte FoldAscii(byte value) =>
        value is >= (byte)'A' and <= (byte)'Z' ? (byte)(value + 32) : value;
}
