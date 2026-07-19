using ExpressThat.LovelyGit.Services.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Configuration;

internal sealed class GitIdentityConfigParser
{
    private const int MaximumIncludeDepth = 16;
    private readonly string _gitDirectory;
    private readonly string? _branchName;
    private readonly string? _homeDirectory;
    private readonly HashSet<string> _activeIncludes = new(StringComparer.OrdinalIgnoreCase);

    public GitIdentityConfigParser(
        string gitDirectory,
        string? branchName,
        string? homeDirectory)
    {
        _gitDirectory = gitDirectory;
        _branchName = branchName;
        _homeDirectory = homeDirectory;
    }

    public Task<bool> ReadAsync(
        string path,
        GitIdentityValueSource source,
        GitIdentityAccumulator identity,
        CancellationToken cancellationToken,
        bool detectWorktreeConfig = false) =>
        ReadAsync(path, source, identity, 0, cancellationToken, detectWorktreeConfig);

    private async Task<bool> ReadAsync(
        string path,
        GitIdentityValueSource source,
        GitIdentityAccumulator identity,
        int depth,
        CancellationToken cancellationToken,
        bool detectWorktreeConfig)
    {
        if (depth >= MaximumIncludeDepth || !File.Exists(path)) return false;

        var fullPath = Path.GetFullPath(path);
        if (!_activeIncludes.Add(fullPath)) return false;

        try
        {
            using var trace = LovelyGitTrace.Time(
                "git.identity.config", $"{source} {Path.GetFileName(fullPath)}");
            var state = new ConfigFileState(
                this, fullPath, detectWorktreeConfig);
            await PooledTextLineReader.ReadAsync(
                fullPath,
                state,
                static (line, fileState) => fileState.ProcessLine(line),
                cancellationToken).ConfigureAwait(false);
            await ApplyOperationsAsync(
                state.Operations, source, identity, depth, cancellationToken)
                .ConfigureAwait(false);
            return state.WorktreeConfigEnabled;
        }
        finally
        {
            _activeIncludes.Remove(fullPath);
        }
    }

    private async Task ApplyOperationsAsync(
        List<ConfigOperation> operations,
        GitIdentityValueSource source,
        GitIdentityAccumulator identity,
        int depth,
        CancellationToken cancellationToken)
    {
        foreach (var operation in operations)
        {
            switch (operation.Kind)
            {
                case ConfigOperationKind.Name:
                    identity.ApplyName(operation.Value, source);
                    break;
                case ConfigOperationKind.Email:
                    identity.ApplyEmail(operation.Value, source);
                    break;
                case ConfigOperationKind.Include:
                    await ReadAsync(
                        operation.Value, source, identity, depth + 1,
                        cancellationToken, detectWorktreeConfig: false)
                        .ConfigureAwait(false);
                    break;
            }
        }
    }

    private sealed class ConfigFileState
    {
        private readonly GitIdentityConfigParser _parser;
        private readonly string _path;
        private readonly bool _detectWorktreeConfig;
        private GitIdentityConfigSection _section;
        private string? _subsection;
        private bool _worktreeConfigObserved;

        public ConfigFileState(
            GitIdentityConfigParser parser,
            string path,
            bool detectWorktreeConfig)
        {
            _parser = parser;
            _path = path;
            _detectWorktreeConfig = detectWorktreeConfig;
        }

        public List<ConfigOperation> Operations { get; } = [];
        public bool WorktreeConfigEnabled { get; private set; }

        public void ProcessLine(ReadOnlySpan<char> rawLine)
        {
            var line = rawLine.Trim();
            if (line.IsEmpty || line[0] is '#' or ';') return;
            if (GitIdentityConfigSyntax.TryReadSection(
                    line, out var section, out var subsection))
            {
                _section = section;
                _subsection = subsection;
                return;
            }

            if (_section == GitIdentityConfigSection.Other ||
                !GitIdentityConfigSyntax.TryReadValue(line, out var key, out var value))
            {
                return;
            }

            if (_section == GitIdentityConfigSection.User)
            {
                AddIdentity(key, value);
            }
            else if (IsMatchingInclude(key))
            {
                var includePath = GitConfigConditionMatcher.ResolveIncludePath(
                    GitIdentityConfigSyntax.Unquote(value), _parser._homeDirectory, _path);
                Operations.Add(new(ConfigOperationKind.Include, includePath));
            }
            else if (_detectWorktreeConfig && !_worktreeConfigObserved &&
                     _section == GitIdentityConfigSection.Extensions &&
                     key.Equals("worktreeconfig", StringComparison.OrdinalIgnoreCase))
            {
                _worktreeConfigObserved = true;
                WorktreeConfigEnabled = GitIdentityConfigSyntax.IsEnabled(value);
            }
        }

        private void AddIdentity(ReadOnlySpan<char> key, ReadOnlySpan<char> value)
        {
            var kind = key.Equals("name", StringComparison.OrdinalIgnoreCase)
                ? ConfigOperationKind.Name
                : key.Equals("email", StringComparison.OrdinalIgnoreCase)
                    ? ConfigOperationKind.Email
                    : ConfigOperationKind.None;
            if (kind != ConfigOperationKind.None)
            {
                Operations.Add(new(kind, GitIdentityConfigSyntax.Unquote(value)));
            }
        }

        private bool IsMatchingInclude(ReadOnlySpan<char> key)
        {
            if (!key.Equals("path", StringComparison.OrdinalIgnoreCase)) return false;
            if (_section == GitIdentityConfigSection.Include) return true;
            return _section == GitIdentityConfigSection.IncludeIf &&
                   _subsection is not null &&
                   GitConfigConditionMatcher.Matches(
                       _subsection, _parser._gitDirectory, _parser._branchName,
                       _parser._homeDirectory, _path);
        }
    }

    private readonly record struct ConfigOperation(ConfigOperationKind Kind, string Value);

    private enum ConfigOperationKind
    {
        None,
        Name,
        Email,
        Include,
    }
}
