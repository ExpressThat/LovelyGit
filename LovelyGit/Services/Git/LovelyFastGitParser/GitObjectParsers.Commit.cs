using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal static partial class GitObjectParsers
{
    public static GitCommit ParseCommit(
        GitObjectId id,
        ReadOnlySpan<byte> data,
        bool includeBody = true,
        bool includeDisplayText = true)
    {
        var bodyStart = FindBodyStart(data);
        var header = bodyStart >= 0 ? data[..(bodyStart - 2)] : data;
        var body = bodyStart >= 0 ? data[bodyStart..] : ReadOnlySpan<byte>.Empty;
        var commit = new GitCommit
        {
            Hash = id,
            Body = includeBody && body.Length > 0 ? Encoding.UTF8.GetString(body) : string.Empty,
            SignatureKind = ReadSignatureKind(header),
        };

        foreach (var rawLine in EnumerateByteLines(header))
        {
            var line = TrimTrailingCarriageReturn(rawLine);
            if (line.StartsWith("parent "u8))
            {
                commit.AddParentHash(ParseObjectId(line["parent "u8.Length..], id.ObjectFormat));
            }
            else if (includeBody && line.StartsWith("tree "u8))
            {
                commit.TreeHash = ParseObjectId(line["tree "u8.Length..], id.ObjectFormat);
            }
            else if (line.StartsWith("author "u8))
            {
                commit.AuthorUnixSeconds = ApplySignature(
                    commit,
                    line["author "u8.Length..],
                    includeName: includeDisplayText,
                    includeEmail: includeBody);
            }
            else if (line.StartsWith("committer "u8))
            {
                commit.CommitterUnixSeconds = ApplySignature(
                    commit,
                    line["committer "u8.Length..],
                    includeName: false,
                    includeEmail: false);
            }
        }

        if (commit.CommitterUnixSeconds == 0)
        {
            commit.CommitterUnixSeconds = commit.AuthorUnixSeconds;
        }

        if (includeDisplayText)
        {
            commit.Subject = ParseSubject(body);
        }

        return commit;
    }

    private static GitSignatureKind ReadSignatureKind(ReadOnlySpan<byte> header)
    {
        if (header.IndexOf("\ngpgsig "u8) < 0 &&
            !header.StartsWith("gpgsig "u8) &&
            header.IndexOf("\ngpgsig-sha256 "u8) < 0 &&
            !header.StartsWith("gpgsig-sha256 "u8))
        {
            return GitSignatureKind.None;
        }

        if (header.IndexOf("BEGIN SSH SIGNATURE"u8) >= 0)
        {
            return GitSignatureKind.Ssh;
        }

        if (header.IndexOf("BEGIN PGP SIGNATURE"u8) >= 0)
        {
            return GitSignatureKind.OpenPgp;
        }

        if (header.IndexOf("BEGIN SIGNED MESSAGE"u8) >= 0 ||
            header.IndexOf("BEGIN CERTIFICATE"u8) >= 0)
        {
            return GitSignatureKind.X509;
        }

        return GitSignatureKind.Unknown;
    }

    private static int FindBodyStart(ReadOnlySpan<byte> data)
    {
        for (var i = 1; i < data.Length; i++)
        {
            if (data[i - 1] == (byte)'\n' && data[i] == (byte)'\n')
            {
                return i + 1;
            }
        }

        return -1;
    }

    private static GitObjectId ParseObjectId(ReadOnlySpan<byte> value, GitObjectFormat objectFormat)
    {
        value = TrimAscii(value);
        return GitObjectId.ParseAscii(value, objectFormat);
    }

    private static long ApplySignature(
        GitCommit commit,
        ReadOnlySpan<byte> value,
        bool includeName,
        bool includeEmail)
    {
        value = TrimAscii(value);
        var emailStart = value.LastIndexOf((byte)'<');
        var emailEnd = value.LastIndexOf((byte)'>');
        if (emailStart < 0 || emailEnd <= emailStart)
        {
            if (includeName)
            {
                commit.AuthorName = Encoding.UTF8.GetString(value);
            }

            return 0;
        }

        if (includeName)
        {
            commit.AuthorName = Encoding.UTF8.GetString(TrimAscii(value[..emailStart]));
        }

        if (includeEmail)
        {
            commit.AuthorEmail = Encoding.UTF8.GetString(TrimAscii(value[(emailStart + 1)..emailEnd]));
        }

        return ParseLeadingInt64(TrimAscii(value[(emailEnd + 1)..]));
    }

    private static long ParseLeadingInt64(ReadOnlySpan<byte> value)
    {
        long result = 0;
        var sign = 1;
        var index = 0;
        if (value.Length > 0 && value[0] == (byte)'-')
        {
            sign = -1;
            index = 1;
        }

        for (; index < value.Length; index++)
        {
            var current = value[index];
            if (current < (byte)'0' || current > (byte)'9')
            {
                break;
            }

            result = (result * 10) + current - (byte)'0';
        }

        return result * sign;
    }

    private static string ParseSubject(ReadOnlySpan<byte> body)
    {
        body = TrimLeadingBlankLines(body);
        var newline = body.IndexOf((byte)'\n');
        var subject = newline >= 0 ? TrimTrailingCarriageReturn(body[..newline]) : body;
        return subject.Length == 0 ? string.Empty : Encoding.UTF8.GetString(subject);
    }

    private static ReadOnlySpan<byte> TrimLeadingBlankLines(ReadOnlySpan<byte> value)
    {
        while (value.Length > 0 && (value[0] == (byte)'\n' || value[0] == (byte)'\r'))
        {
            value = value[1..];
        }

        return value;
    }

    private static ReadOnlySpan<byte> TrimAscii(ReadOnlySpan<byte> value)
    {
        while (value.Length > 0 && value[0] <= (byte)' ')
        {
            value = value[1..];
        }

        while (value.Length > 0 && value[^1] <= (byte)' ')
        {
            value = value[..^1];
        }

        return value;
    }

    private static ReadOnlySpan<byte> TrimTrailingCarriageReturn(ReadOnlySpan<byte> value)
    {
        return value.Length > 0 && value[^1] == (byte)'\r' ? value[..^1] : value;
    }

    private static ByteLineEnumerator EnumerateByteLines(ReadOnlySpan<byte> text)
    {
        return new ByteLineEnumerator(text);
    }

    private ref struct ByteLineEnumerator
    {
        private ReadOnlySpan<byte> _remaining;

        public ByteLineEnumerator(ReadOnlySpan<byte> text)
        {
            Current = default;
            _remaining = text;
        }

        public ReadOnlySpan<byte> Current { get; private set; }

        public ByteLineEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_remaining.Length == 0)
            {
                return false;
            }

            var newlineIndex = _remaining.IndexOf((byte)'\n');
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
