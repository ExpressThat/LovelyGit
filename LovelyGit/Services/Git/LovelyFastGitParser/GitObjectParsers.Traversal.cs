namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal static partial class GitObjectParsers
{
    public static GitCommitTraversalHeader ParseCommitTraversalHeader(
        GitObjectId id,
        ReadOnlySpan<byte> data)
    {
        var bodyStart = FindBodyStart(data);
        var header = bodyStart >= 0 ? data[..(bodyStart - 2)] : data;
        var treeHash = default(GitObjectId?);
        var firstParent = default(GitObjectId);
        List<GitObjectId>? additionalParents = null;
        var parentCount = 0;
        var authorUnixSeconds = 0L;
        foreach (var rawLine in EnumerateByteLines(header))
        {
            var line = TrimTrailingCarriageReturn(rawLine);
            if (line.StartsWith("tree "u8))
            {
                treeHash = ParseObjectId(line["tree "u8.Length..], id.ObjectFormat);
            }
            else if (line.StartsWith("parent "u8))
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
                var emailEnd = author.LastIndexOf((byte)'>');
                authorUnixSeconds = emailEnd >= 0
                    ? ParseLeadingInt64(TrimAscii(author[(emailEnd + 1)..]))
                    : 0;
            }
        }

        return new GitCommitTraversalHeader(
            treeHash,
            firstParent,
            additionalParents?.ToArray(),
            parentCount,
            authorUnixSeconds);
    }
}
