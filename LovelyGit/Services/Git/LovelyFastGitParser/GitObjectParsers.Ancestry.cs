namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal static partial class GitObjectParsers
{
    public static GitCommitAncestryHeader ParseCommitAncestryHeader(
        GitObjectId id,
        ReadOnlySpan<byte> data)
    {
        var bodyStart = FindBodyStart(data);
        var header = bodyStart >= 0 ? data[..(bodyStart - 2)] : data;
        var treeHash = default(GitObjectId?);
        var firstParent = default(GitObjectId);
        List<GitObjectId>? additionalParents = null;
        var parentCount = 0;
        var commitUnixSeconds = 0L;
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
                if (parentCount++ == 0) firstParent = parent;
                else (additionalParents ??= new List<GitObjectId>(1)).Add(parent);
            }
            else if (line.StartsWith("committer "u8))
            {
                var committer = TrimAscii(line["committer "u8.Length..]);
                var emailEnd = committer.LastIndexOf((byte)'>');
                commitUnixSeconds = emailEnd >= 0
                    ? ParseLeadingInt64(TrimAscii(committer[(emailEnd + 1)..]))
                    : 0;
            }
        }

        return new GitCommitAncestryHeader(
            treeHash,
            firstParent,
            additionalParents?.ToArray(),
            parentCount,
            commitUnixSeconds);
    }
}
