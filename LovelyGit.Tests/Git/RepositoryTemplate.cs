using System.Collections.Concurrent;

namespace LovelyGit.Tests.Git;

internal sealed class RepositoryTemplate<TState>
{
    private readonly ConcurrentQueue<DirectoryInfo> _preparedCopies = new();
    private readonly Lazy<TemplateState> _template;
    private Exception? _prewarmFailure;
    private ManualResetEventSlim? _prewarmFinished;

    public RepositoryTemplate(
        string templatePrefix,
        Func<DirectoryInfo, TState> initialize,
        int prewarmCopies = 0)
    {
        _template = new Lazy<TemplateState>(
            () => CreateTemplate(templatePrefix, initialize),
            LazyThreadSafetyMode.ExecutionAndPublication);
        if (prewarmCopies > 0) RegisterPrewarm(prewarmCopies);
    }

    public void WarmUp() => _ = _template.Value;

    public (DirectoryInfo Directory, TState State) CreateCopy(string copyPrefix)
    {
        _prewarmFinished?.Wait();
        if (_prewarmFailure != null)
        {
            throw new InvalidOperationException("Repository template prewarm failed.", _prewarmFailure);
        }
        var template = _template.Value;
        if (!_preparedCopies.TryDequeue(out var copy))
        {
            copy = Directory.CreateTempSubdirectory(copyPrefix);
            CopyDirectory(template.Directory, copy);
        }
        return (copy, template.State);
    }

    public TState CopyInto(DirectoryInfo destination)
    {
        var template = _template.Value;
        CopyDirectory(template.Directory, destination);
        return template.State;
    }

    internal void PrepareCopies(int copyCount)
    {
        var template = _template.Value;
        for (var index = 0; index < copyCount; index++)
        {
            var copy = Directory.CreateTempSubdirectory("lovelygit-prewarmed-copy-");
            CopyDirectory(template.Directory, copy);
            _preparedCopies.Enqueue(copy);
        }
    }

    private static TemplateState CreateTemplate(
        string prefix,
        Func<DirectoryInfo, TState> initializer)
    {
        var directory = Directory.CreateTempSubdirectory(prefix);
        return new TemplateState(directory, initializer(directory));
    }

    private void RegisterPrewarm(int copyCount)
    {
        var finished = new ManualResetEventSlim();
        if (!PerformanceTemplatePrewarmer.Register(() => Prewarm(copyCount, finished)))
        {
            finished.Dispose();
            return;
        }
        _prewarmFinished = finished;
    }

    private void Prewarm(int copyCount, ManualResetEventSlim finished)
    {
        try
        {
            PrepareCopies(copyCount);
        }
        catch (Exception exception)
        {
            _prewarmFailure = exception;
        }
        finally
        {
            finished.Set();
        }
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
