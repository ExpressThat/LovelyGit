using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionService
{
    private bool TryReadMetadataCached(
        string repositoryPath,
        string path,
        bool ignoreWhitespace,
        ConflictReadTrace readTrace,
        out ConflictResolutionResponse response)
    {
        if (_responseCache.TryGetCurrent(
                repositoryPath,
                path,
                ignoreWhitespace,
                out response,
                out _))
        {
            readTrace.Mark("metadata-cache");
            return true;
        }

        if (!_responseCache.TryGetCurrentSibling(
                repositoryPath,
                path,
                ignoreWhitespace,
                out var sibling,
                out var siblingStamp,
                out var retainedTexts))
        {
            response = null!;
            return false;
        }

        response = BuildCachedVariant(sibling, retainedTexts, ignoreWhitespace);
        _responseCache.Set(
            repositoryPath,
            path,
            response.WorktreeFingerprint,
            ignoreWhitespace,
            response,
            siblingStamp,
            retainedTexts);
        readTrace.Mark("metadata-sibling");
        return true;
    }
}
