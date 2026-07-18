using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace LovelyGit.Tests.Git;

internal static class PerformanceTemplatePrewarmer
{
    private const int MaximumWorkers = 4;
    private static readonly string[] PreferredOrder =
    [
        "NativeGitBisectStateReaderPerformanceTests",
        "NativeBranchComparisonPerformanceTests",
        "NativeAncestryWorkflowPerformanceTests",
        "RepositoryRefsPerformanceTests",
        "ConflictResolutionReadPerformanceTests",
        "CommitFileDiffPerformanceTests",
        "NativeInteractiveRebasePlanPerformanceTests",
        "GitCommitExistencePerformanceTests",
        "StagedDiffPerformanceTests",
        "UnstagedDiffPerformanceTests",
        "UntrackedDiffPerformanceTests",
    ];
    private static readonly ManualResetEventSlim WorkersFinished = new(initialState: true);
    private static ConcurrentQueue<Action>? _pending;
    private static int _workersRemaining;
    private static volatile bool _discovering;

    [ModuleInitializer]
    internal static void Start()
    {
        if (Environment.GetEnvironmentVariable(
                "LOVELYGIT_PREWARM_PERFORMANCE_TEMPLATES") != "1")
        {
            return;
        }
        _pending = new ConcurrentQueue<Action>();
        _discovering = true;
        try
        {
            foreach (var type in FindTemplateOwners().OrderBy(PrewarmOrder))
            {
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }
        }
        finally
        {
            _discovering = false;
        }
        StartWorkers();
    }

    internal static bool Register(Action warmUp)
    {
        if (!_discovering) return false;
        _pending!.Enqueue(warmUp);
        return true;
    }

    internal static void WaitForCompletion() => WorkersFinished.Wait();

    private static void StartWorkers()
    {
        var workerCount = Math.Min(MaximumWorkers, _pending!.Count);
        if (workerCount == 0) return;
        _workersRemaining = workerCount;
        WorkersFinished.Reset();
        for (var index = 0; index < workerCount; index++)
        {
            var worker = new Thread(() =>
            {
                try
                {
                    while (_pending.TryDequeue(out var action))
                    {
                        try
                        {
                            action();
                        }
                        catch
                        {
                            // Lazy template access will surface initialization failures to its test.
                        }
                    }
                }
                finally
                {
                    if (Interlocked.Decrement(ref _workersRemaining) == 0)
                        WorkersFinished.Set();
                }
            })
            {
                IsBackground = true,
                Name = $"LovelyGit test fixture prewarm {index + 1}",
            };
            worker.Start();
        }
    }

    private static IEnumerable<Type> FindTemplateOwners() =>
        typeof(PerformanceTemplatePrewarmer).Assembly.GetTypes().Where(type =>
            type.Name.EndsWith("PerformanceTests", StringComparison.Ordinal) &&
            type.GetFields(BindingFlags.NonPublic | BindingFlags.Static).Any(field =>
                field.FieldType.IsGenericType &&
                field.FieldType.GetGenericTypeDefinition() == typeof(RepositoryTemplate<>)));

    private static int PrewarmOrder(Type type)
    {
        var index = Array.IndexOf(PreferredOrder, type.Name);
        return index < 0 ? PreferredOrder.Length : index;
    }
}
