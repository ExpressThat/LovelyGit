namespace LovelyGit.Tests.Git;

internal sealed class RepositoryTemplate<TState>(
    string templatePrefix,
    Func<DirectoryInfo, TState> initialize)
{
    private readonly Lazy<TemplateState> _template = new(
        () => CreateTemplate(templatePrefix, initialize),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public (DirectoryInfo Directory, TState State) CreateCopy(string copyPrefix)
    {
        var template = _template.Value;
        var copy = Directory.CreateTempSubdirectory(copyPrefix);
        CopyDirectory(template.Directory, copy);
        return (copy, template.State);
    }

    public TState CopyInto(DirectoryInfo destination)
    {
        var template = _template.Value;
        CopyDirectory(template.Directory, destination);
        return template.State;
    }

    private static TemplateState CreateTemplate(
        string prefix,
        Func<DirectoryInfo, TState> initializer)
    {
        var directory = Directory.CreateTempSubdirectory(prefix);
        return new TemplateState(directory, initializer(directory));
    }

    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
    {
        foreach (var directory in source.EnumerateDirectories("*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(
                destination.FullName,
                Path.GetRelativePath(source.FullName, directory.FullName)));
        }

        foreach (var file in source.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.CopyTo(Path.Combine(
                destination.FullName,
                Path.GetRelativePath(source.FullName, file.FullName)));
        }
    }

    private sealed record TemplateState(DirectoryInfo Directory, TState State);
}
