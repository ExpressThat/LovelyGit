using ExpressThat.LovelyGit.Services.Git.Reset;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class UndoLastCommitService
{
    private readonly HeadCommitMessageService _headMessages;
    private readonly GitResetCommandService _reset;

    public UndoLastCommitService(
        HeadCommitMessageService headMessages,
        GitResetCommandService reset)
    {
        _headMessages = headMessages;
        _reset = reset;
    }

    public async Task<HeadCommitMessageResponse> UndoAsync(
        string repositoryPath,
        string expectedHeadHash,
        CancellationToken cancellationToken)
    {
        var head = await _headMessages.GetAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        if (!string.Equals(head.Hash, expectedHeadHash.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "HEAD changed after the undo confirmation opened. Review the latest commit and try again.");
        }

        if (head.FirstParentHash is null)
        {
            throw new InvalidOperationException(
                "The initial commit cannot be undone because it has no parent commit.");
        }

        await _reset.UndoLastCommitAsync(
                repositoryPath,
                head.Hash,
                head.FirstParentHash,
                cancellationToken)
            .ConfigureAwait(false);
        return head;
    }
}
