using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository : IDisposable
{
    private static void AddRef(Dictionary<GitObjectId, List<GitCommitRef>> map, GitObjectId id, string name, GitRefKind kind)
    {
        if (!map.TryGetValue(id, out var refs))
        {
            refs = new List<GitCommitRef>();
            map[id] = refs;
        }

        var reference = new GitCommitRef(name, kind);
        if (!refs.Contains(reference))
        {
            refs.Add(reference);
        }
    }

    private static void AddRemotePrefix(HashSet<string> remotePrefixes, string displayName)
    {
        var slashIndex = displayName.IndexOf('/');
        if (slashIndex <= 0)
        {
            return;
        }

        var remoteName = displayName[..slashIndex];
        if (!remoteName.Equals("HEAD", StringComparison.Ordinal))
        {
            remotePrefixes.Add(remoteName);
        }
    }
}
