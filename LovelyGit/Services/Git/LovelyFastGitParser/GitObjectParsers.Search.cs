using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal static partial class GitObjectParsers
{
    public static GitCommitSearchHeader ParseCommitSearchHeader(
        GitObjectId id,
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> queryUtf8,
        string query,
        ReadOnlySpan<byte> authorQueryUtf8,
        string authorQuery)
    {
        var bodyStart = FindBodyStart(data);
        var header = bodyStart >= 0 ? data[..(bodyStart - 2)] : data;
        var body = bodyStart >= 0 ? data[bodyStart..] : ReadOnlySpan<byte>.Empty;
        var firstParent = default(GitObjectId);
        List<GitObjectId>? additionalParents = null;
        var parentCount = 0;
        var authorUnixSeconds = 0L;
        var identityMatches = false;
        var authorMatches = authorQueryUtf8.IsEmpty;
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
                var identity = GetAuthorIdentity(author);
                identityMatches = ContainsUtf8IgnoreCase(identity, queryUtf8);
                if (!authorQueryUtf8.IsEmpty)
                {
                    authorMatches = ContainsUtf8IgnoreCase(identity, authorQueryUtf8);
                    if (!authorMatches && ContainsNonAscii(authorQueryUtf8))
                    {
                        authorMatches = Encoding.UTF8.GetString(identity)
                            .Contains(authorQuery, StringComparison.OrdinalIgnoreCase);
                    }
                }
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
            queryUtf8.IsEmpty || identityMatches || messageMatches,
            authorMatches);
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

        var first = FoldAscii(query[0]);
        var alternate = first is >= (byte)'a' and <= (byte)'z'
            ? (byte)(first - 32)
            : first;
        var lastStart = source.Length - query.Length;
        var searchStart = 0;
        while (searchStart <= lastStart)
        {
            var possibleStarts = source.Slice(searchStart, lastStart - searchStart + 1);
            var relativeStart = first == alternate
                ? possibleStarts.IndexOf(first)
                : possibleStarts.IndexOfAny(first, alternate);
            if (relativeStart < 0)
            {
                return false;
            }

            var start = searchStart + relativeStart;
            var matched = true;
            for (var offset = 1; offset < query.Length; offset++)
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

            searchStart = start + 1;
        }

        return false;
    }

    private static byte FoldAscii(byte value) =>
        value is >= (byte)'A' and <= (byte)'Z' ? (byte)(value + 32) : value;
}
