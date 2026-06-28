namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository
{
    public IReadOnlyList<GitRef> GetRefs() =>
        _refsByFullName.Values
            .Where(reference => reference.Kind is GitRefKind.Head or GitRefKind.Remote or GitRefKind.Tag or GitRefKind.Stash)
            .OrderBy(reference => reference.Kind)
            .ThenBy(reference => reference.Name, StringComparer.Ordinal)
            .ToList();
}
