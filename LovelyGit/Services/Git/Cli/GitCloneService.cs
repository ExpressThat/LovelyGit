using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using CliWrap;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed partial class GitCloneService
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeClones = new();
    private readonly GitCliService _gitCliService;

    public GitCloneService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task<string> CloneAsync(
        Guid operationId,
        string remoteUrl,
        string parentPath,
        string directoryName,
        bool shallow,
        bool recurseSubmodules,
        Action<GitCloneProgress> onProgress,
        CancellationToken cancellationToken)
    {
        if (operationId == Guid.Empty)
        {
            throw new ArgumentException("OperationId is required.", nameof(operationId));
        }

        var normalizedRemote = NormalizeRemote(remoteUrl);
        var normalizedParent = NormalizeParentPath(parentPath);
        var normalizedName = NormalizeDirectoryName(directoryName);
        var destinationPath = ResolveDestinationPath(normalizedParent, normalizedName);
        if (Directory.Exists(destinationPath) || File.Exists(destinationPath))
        {
            throw new InvalidOperationException("The clone destination already exists.");
        }

        using var cloneCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (!_activeClones.TryAdd(operationId, cloneCancellation))
        {
            throw new InvalidOperationException("A clone with this operation id is already running.");
        }

        var progress = new CloneProgressReporter(onProgress);
        try
        {
            progress.Report(new GitCloneProgress("Preparing", "Preparing destination", null));
            var arguments = BuildArguments(
                normalizedRemote,
                destinationPath,
                shallow,
                recurseSubmodules);
            var command = _gitCliService
                .CreateCommand(arguments, normalizedParent, validateExitCode: false)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(progress.ReportLine))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(progress.ReportLine));

            var result = await command.ExecuteAsync(cloneCancellation.Token).ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    progress.LastMessage ?? "Git clone failed.");
            }

            if (!Directory.Exists(Path.Combine(destinationPath, ".git")))
            {
                throw new InvalidDataException("Git clone completed without creating a repository.");
            }

            progress.Report(new GitCloneProgress("Complete", "Clone complete", 100), force: true);
            return destinationPath;
        }
        catch (Exception cloneFailure)
        {
            await DeletePartialDestinationAsync(destinationPath, cloneFailure)
                .ConfigureAwait(false);
            throw;
        }
        finally
        {
            _activeClones.TryRemove(
                new KeyValuePair<Guid, CancellationTokenSource>(operationId, cloneCancellation));
        }
    }

    public bool Cancel(Guid operationId)
    {
        if (!_activeClones.TryGetValue(operationId, out var cancellation))
        {
            return false;
        }

        cancellation.Cancel();
        return true;
    }

    private static IReadOnlyList<string> BuildArguments(
        string remoteUrl,
        string destinationPath,
        bool shallow,
        bool recurseSubmodules)
    {
        var arguments = new List<string> { "clone", "--progress" };
        if (shallow)
        {
            arguments.Add("--depth=1");
        }

        if (recurseSubmodules)
        {
            arguments.Add("--recurse-submodules");
            if (shallow)
            {
                arguments.Add("--shallow-submodules");
            }
        }

        arguments.Add("--");
        arguments.Add(remoteUrl);
        arguments.Add(destinationPath);
        return arguments;
    }

    private static string NormalizeRemote(string remoteUrl)
    {
        var normalized = remoteUrl.Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Repository URL is required.", nameof(remoteUrl));
        }

        if (normalized.Length > 4096 || normalized.Any(char.IsControl))
        {
            throw new ArgumentException("Repository URL is not valid.", nameof(remoteUrl));
        }

        return normalized;
    }

    private static string NormalizeParentPath(string parentPath)
    {
        var normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parentPath.Trim()));
        if (!Directory.Exists(normalized))
        {
            throw new DirectoryNotFoundException("The destination folder does not exist.");
        }

        return normalized;
    }

    private static string NormalizeDirectoryName(string directoryName)
    {
        var normalized = directoryName.Trim();
        if (normalized.Length == 0 ||
            normalized is "." or ".." ||
            normalized.Length > 255 ||
            normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            normalized.Contains(Path.DirectorySeparatorChar) ||
            normalized.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("Repository folder name is not valid.", nameof(directoryName));
        }

        return normalized;
    }

    private static string ResolveDestinationPath(string parentPath, string directoryName)
    {
        var destinationPath = Path.GetFullPath(Path.Combine(parentPath, directoryName));
        var destinationParent = Path.GetDirectoryName(destinationPath);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!string.Equals(destinationParent, parentPath, comparison))
        {
            throw new ArgumentException("Repository destination must be inside the selected folder.");
        }

        return destinationPath;
    }

    private sealed partial class CloneProgressReporter
    {
        private static readonly TimeSpan MinimumUpdateInterval = TimeSpan.FromMilliseconds(80);
        private readonly object _gate = new();
        private readonly Action<GitCloneProgress> _onProgress;
        private int _highestOverallPercent;
        private long _lastReportTimestamp;
        private string? _lastStage;

        public CloneProgressReporter(Action<GitCloneProgress> onProgress)
        {
            _onProgress = onProgress;
        }

        public string? LastMessage { get; private set; }

        public void ReportLine(string line)
        {
            foreach (var segment in line.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var message = segment.StartsWith("remote: ", StringComparison.Ordinal)
                    ? segment["remote: ".Length..]
                    : segment;
                if (message.Length == 0)
                {
                    continue;
                }

                LastMessage = message;
                if (message.StartsWith("Total ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var percentMatch = PercentRegex().Match(message);
                int? percent = percentMatch.Success &&
                    int.TryParse(percentMatch.Groups[1].Value, out var parsed)
                        ? Math.Clamp(parsed, 0, 100)
                        : null;
                Report(new GitCloneProgress(GetStage(message), message, percent));
            }
        }

    }

    [GeneratedRegex("([0-9]{1,3})%", RegexOptions.CultureInvariant)]
    private static partial Regex PercentRegex();
}
