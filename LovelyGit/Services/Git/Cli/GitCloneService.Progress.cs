using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed partial class GitCloneService
{
    private sealed partial class CloneProgressReporter
    {
        public void Report(GitCloneProgress progress, bool force = false)
        {
            GitCloneProgress overallProgress;
            lock (_gate)
            {
                var phasePercent = progress.Percent;
                var mappedPercent = MapOverallPercent(progress.Stage, progress.Percent);
                _highestOverallPercent = Math.Max(_highestOverallPercent, mappedPercent);
                overallProgress = progress with
                {
                    Percent = _highestOverallPercent,
                    PhasePercent = phasePercent,
                };

                var now = Stopwatch.GetTimestamp();
                var elapsed = Stopwatch.GetElapsedTime(_lastReportTimestamp, now);
                if (!force &&
                    string.Equals(overallProgress.Stage, _lastStage, StringComparison.Ordinal) &&
                    elapsed < MinimumUpdateInterval)
                {
                    return;
                }

                _lastReportTimestamp = now;
                _lastStage = overallProgress.Stage;
            }

            _onProgress(overallProgress);
        }

        private static int MapOverallPercent(string stage, int? phasePercent)
        {
            if (stage.Equals("Complete", StringComparison.OrdinalIgnoreCase))
            {
                return 100;
            }

            if (stage.StartsWith("Preparing", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (stage.Equals("Cloning repository", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (stage.StartsWith("Enumerating objects", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (stage.StartsWith("Counting objects", StringComparison.OrdinalIgnoreCase))
            {
                return MapPhasePercent(phasePercent, 2, 8);
            }

            if (stage.StartsWith("Compressing objects", StringComparison.OrdinalIgnoreCase))
            {
                return MapPhasePercent(phasePercent, 8, 20);
            }

            if (stage.StartsWith("Receiving objects", StringComparison.OrdinalIgnoreCase))
            {
                return MapPhasePercent(phasePercent, 20, 70);
            }

            if (stage.StartsWith("Resolving deltas", StringComparison.OrdinalIgnoreCase))
            {
                return MapPhasePercent(phasePercent, 70, 80);
            }

            if (stage.StartsWith("Updating files", StringComparison.OrdinalIgnoreCase) ||
                stage.StartsWith("Checking out files", StringComparison.OrdinalIgnoreCase) ||
                stage.StartsWith("Filtering content", StringComparison.OrdinalIgnoreCase))
            {
                return MapPhasePercent(phasePercent, 80, 99);
            }

            return 0;
        }

        private static int MapPhasePercent(int? phasePercent, int start, int end)
        {
            var normalized = Math.Clamp(phasePercent ?? 0, 0, 100);
            return start + ((end - start) * normalized / 100);
        }

        private static string GetStage(string message)
        {
            if (message.StartsWith("Cloning into ", StringComparison.OrdinalIgnoreCase))
            {
                return "Cloning repository";
            }

            var colonIndex = message.IndexOf(':');
            var stage = colonIndex > 0 ? message[..colonIndex] : message;
            return stage.Length > 48 ? stage[..48] : stage;
        }
    }
}

internal sealed record GitCloneProgress(
    string Stage,
    string Message,
    int? Percent,
    int? PhasePercent = null);
