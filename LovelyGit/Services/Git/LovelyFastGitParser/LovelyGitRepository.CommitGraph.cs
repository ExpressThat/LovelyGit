using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.CommitGraph;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository
{
    private GitCommitGraphReader? GetCommitGraph()
    {
        if (_commitGraphInitialized) return _commitGraph;
        lock (_commitGraphGate)
        {
            if (_commitGraphInitialized) return _commitGraph;
            _commitGraph = GitCommitGraphReader.TryOpen(GitDirectory, ObjectFormat);
            _commitGraphInitialized = true;
            return _commitGraph;
        }
    }
}
