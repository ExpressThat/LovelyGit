using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository : IDisposable
{
    private static void AddName(Dictionary<GitObjectId, List<string>> map, GitObjectId id, string name)
    {
        if (!map.TryGetValue(id, out var names))
        {
            names = new List<string>();
            map[id] = names;
        }

        if (!names.Contains(name, StringComparer.Ordinal))
        {
            names.Add(name);
        }
    }

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
