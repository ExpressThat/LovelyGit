using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Diagnostics;

internal static class LovelyGitTrace
{
    private static readonly Lock SyncRoot = new();
    private static readonly bool IsEnabled =
        string.Equals(
            Environment.GetEnvironmentVariable("LOVELYGIT_TRACE_NATIVE_MESSAGES"),
            "1",
            StringComparison.Ordinal);

    private static readonly string LogPath = Path.Combine(
        Path.GetTempPath(),
        "LovelyGitNativeTrace.log");

    public static bool Enabled => IsEnabled;

    public static IDisposable Time(string name, string? detail = null)
    {
        return IsEnabled ? new Scope(name, detail) : NoopScope.Instance;
    }

    public static void Write(string name, string? detail = null)
    {
        if (!IsEnabled)
        {
            return;
        }

        WriteLine(name, detail, null);
    }

    private static void WriteLine(string name, string? detail, TimeSpan? elapsed)
    {
        var message = elapsed == null
            ? $"{DateTimeOffset.Now:O} {name} {detail}"
            : $"{DateTimeOffset.Now:O} {name} {elapsed.Value.TotalMilliseconds:F1}ms {detail}";
        lock (SyncRoot)
        {
            File.AppendAllText(LogPath, message + Environment.NewLine);
        }
    }

    private sealed class Scope(string name, string? detail) : IDisposable
    {
        private readonly long startedAt = Stopwatch.GetTimestamp();

        public void Dispose()
        {
            WriteLine(name, detail, Stopwatch.GetElapsedTime(startedAt));
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();

        public void Dispose()
        {
        }
    }
}
