namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository
{
    private const int MaximumTagPeelDepth = 16;

    private static async Task<GitObjectId?> ResolveTagCommitTargetAsync(
        GitObjectStore objectStore,
        GitObjectFormat objectFormat,
        GitObjectId target,
        CancellationToken cancellationToken)
    {
        var current = target;
        for (var depth = 0; depth < MaximumTagPeelDepth; depth++)
        {
            var objectData = await objectStore
                .ReadObjectAsync(current, cancellationToken)
                .ConfigureAwait(false);
            if (objectData.Kind == GitObjectKind.Commit)
            {
                return current;
            }

            if (objectData.Kind != GitObjectKind.Tag)
            {
                return null;
            }

            current = GitObjectParsers
                .ParseTag(current, objectFormat, objectData.Data)
                .Target;
        }

        throw new InvalidDataException(
            $"Annotated tag nesting exceeds {MaximumTagPeelDepth} levels: {target}");
    }
}
