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
        var directory = RepositoryTemplateLifetime.CreateDirectory(prefix);
        var state = initializer(directory);
        RepositoryTemplateLifetime.NormalizeFiles(directory);
        return new TemplateState(directory, state);
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

internal static class RepositoryTemplateLifetime
{
    private const string RootPrefix = "lovelygit-template-process-";
    private const string OwnershipFileName = ".owner";
    private static readonly DirectoryInfo Root = CreateRoot();

    static RepositoryTemplateLifetime() => AppDomain.CurrentDomain.ProcessExit += (_, _) =>
    {
        try
        {
            DeleteDirectory(Root);
        }
        catch (IOException)
        {
            // The gate wrapper reclaims roots after the testhost releases native readers.
        }
        catch (UnauthorizedAccessException)
        {
            // The gate wrapper reclaims roots after the testhost releases native readers.
        }
    };

    public static DirectoryInfo CreateDirectory(string prefix) =>
        Directory.CreateDirectory(Path.Combine(Root.FullName, $"{prefix}{Guid.NewGuid():N}"));

    internal static void DeleteDirectory(DirectoryInfo directory)
    {
        if (!directory.Exists) return;
        try
        {
            directory.Delete(recursive: true);
            return;
        }
        catch (UnauthorizedAccessException)
        {
            directory.Refresh();
            if (!directory.Exists) return;
        }
        NormalizeFiles(directory);
        directory.Delete(recursive: true);
    }

    internal static void NormalizeFiles(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }
    }

    internal static bool TryDeleteRoot(DirectoryInfo directory, TimeSpan minimumAge)
    {
        directory.Refresh();
        if (!directory.Exists || DateTime.UtcNow - directory.CreationTimeUtc < minimumAge)
        {
            return false;
        }
        if (IsOwnerActive(Path.Combine(directory.FullName, OwnershipFileName)))
        {
            return false;
        }
        DeleteDirectory(directory);
        return true;
    }

    private static DirectoryInfo CreateRoot()
    {
        var directory = Directory.CreateTempSubdirectory(RootPrefix);
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        File.WriteAllText(
            Path.Combine(directory.FullName, OwnershipFileName),
            $"{process.Id}|{process.StartTime.ToUniversalTime().Ticks}");
        return directory;
    }

    private static bool IsOwnerActive(string ownerPath)
    {
        if (!File.Exists(ownerPath)) return false;
        var parts = File.ReadAllText(ownerPath).Split('|');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var processId) ||
            !long.TryParse(parts[1], out var startTicks))
        {
            return true;
        }
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);
            return process.StartTime.ToUniversalTime().Ticks == startTicks;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return true;
        }
    }
}
