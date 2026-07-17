using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository
{
    public async Task LoadRefsForCommitAsync(
        GitCommit commit,
        CancellationToken cancellationToken)
    {
        var refs = await GitCommitRefReader.ReadAsync(
                GitDirectory,
                ObjectFormat,
                commit.Hash,
                (target, token) => ResolveTagCommitTargetAsync(
                    _objectStore,
                    ObjectFormat,
                    target,
                    token),
                cancellationToken)
            .ConfigureAwait(false);
        commit.AddRefs(refs);
    }
}
