namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class GitIgnoreMatcher
{
    private readonly List<GitIgnoreRule> _rules;
    private readonly GitIgnoreSourceStamp[] _sourceStamps;

    private GitIgnoreMatcher(List<GitIgnoreRule> rules, GitIgnoreSourceStamp[] sourceStamps)
    {
        _rules = rules;
        _sourceStamps = sourceStamps;
    }

    public static async Task<GitIgnoreMatcher> LoadAsync(
        string workTreeDirectory,
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var rules = new List<GitIgnoreRule>();
        var sources = new List<GitIgnoreSourceStamp>();
        var repositoryConfig = Path.Combine(workTreeDirectory, ".git", "config");
        var userConfig = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".gitconfig");
        sources.Add(GitIgnoreSourceStamp.Read(repositoryConfig));
        sources.Add(GitIgnoreSourceStamp.Read(userConfig));
        var globalExcludes = await LoadGlobalExcludeRulesAsync(
                repositoryConfig,
                userConfig,
                rules,
                cancellationToken)
            .ConfigureAwait(false);
        if (globalExcludes != null)
        {
            sources.Add(GitIgnoreSourceStamp.Read(globalExcludes));
        }

        var infoExcludePath = Path.Combine(gitDirectory, "info", "exclude");
        sources.Add(GitIgnoreSourceStamp.Read(infoExcludePath));
        if (File.Exists(infoExcludePath))
        {
            await LoadRulesFromFileAsync(infoExcludePath, string.Empty, rules, cancellationToken).ConfigureAwait(false);
        }

        var rootGitIgnorePath = Path.Combine(workTreeDirectory, ".gitignore");
        sources.Add(GitIgnoreSourceStamp.Read(rootGitIgnorePath));
        if (File.Exists(rootGitIgnorePath))
        {
            await LoadRulesFromFileAsync(rootGitIgnorePath, string.Empty, rules, cancellationToken).ConfigureAwait(false);
        }

        return new GitIgnoreMatcher(rules, sources.ToArray());
    }

    public async Task LoadRulesForDirectoryAsync(
        string workTreeDirectory,
        string relativeDirectory,
        CancellationToken cancellationToken)
    {
        relativeDirectory = NormalizePath(relativeDirectory);
        if (string.IsNullOrEmpty(relativeDirectory))
        {
            return;
        }

        var gitIgnorePath = Path.Combine(workTreeDirectory, relativeDirectory.Replace('/', Path.DirectorySeparatorChar), ".gitignore");
        if (File.Exists(gitIgnorePath))
        {
            await LoadRulesFromFileAsync(gitIgnorePath, relativeDirectory, _rules, cancellationToken).ConfigureAwait(false);
        }
    }

    public bool IsIgnored(string relativePath, bool isDirectory)
    {
        relativePath = NormalizePath(relativePath).TrimStart('/');
        var parentIgnored = false;
        foreach (var parent in EnumerateParents(relativePath))
        {
            if (IsIgnoredCore(parent, true, allowNegation: false))
            {
                parentIgnored = true;
                break;
            }
        }

        var ignored = parentIgnored;

        foreach (var rule in _rules)
        {
            if (parentIgnored && rule.IsNegation)
            {
                continue;
            }

            if (!rule.IsMatch(relativePath, isDirectory))
            {
                continue;
            }

            ignored = !rule.IsNegation;
        }

        return ignored;
    }

    private bool IsIgnoredCore(string relativePath, bool isDirectory, bool allowNegation)
    {
        var ignored = false;
        foreach (var rule in _rules)
        {
            if (!allowNegation && rule.IsNegation)
            {
                continue;
            }

            if (rule.IsMatch(relativePath, isDirectory))
            {
                ignored = !rule.IsNegation;
            }
        }

        return ignored;
    }

    private static IEnumerable<string> EnumerateParents(string relativePath)
    {
        var index = relativePath.IndexOf('/');
        while (index >= 0)
        {
            yield return relativePath[..index];
            index = relativePath.IndexOf('/', index + 1);
        }
    }

    private static async Task<string?> LoadGlobalExcludeRulesAsync(
        string repositoryConfig,
        string userConfig,
        List<GitIgnoreRule> rules,
        CancellationToken cancellationToken)
    {
        var excludesFile = await TryReadConfigValueAsync(
                repositoryConfig,
                "core",
                "excludesfile",
                cancellationToken)
            .ConfigureAwait(false);
        excludesFile ??= await TryReadConfigValueAsync(
                userConfig,
                "core",
                "excludesfile",
                cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(excludesFile))
        {
            return null;
        }

        excludesFile = ExpandPath(excludesFile);
        if (File.Exists(excludesFile))
        {
            await LoadRulesFromFileAsync(excludesFile, string.Empty, rules, cancellationToken).ConfigureAwait(false);
        }

        return excludesFile;
    }

    private static async Task<string?> TryReadConfigValueAsync(
        string path,
        string sectionName,
        string keyName,
        CancellationToken cancellationToken)
        => await GitIgnoreConfigValueReader.ReadAsync(
            path, sectionName, keyName, cancellationToken).ConfigureAwait(false);

}
