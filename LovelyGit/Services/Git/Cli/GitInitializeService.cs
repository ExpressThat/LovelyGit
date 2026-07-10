using CliWrap.Buffered;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed class GitInitializeService
{
    private readonly GitCliService _gitCliService;

    public GitInitializeService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task<string> InitializeAsync(
        string parentPath,
        string directoryName,
        string initialBranchName,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string?>? commitEnvironment = null)
    {
        var parent = NormalizeParentPath(parentPath);
        var name = NormalizeDirectoryName(directoryName);
        var branch = NormalizeBranchName(initialBranchName);
        var destination = ResolveDestinationPath(parent, name);
        if (Directory.Exists(destination) || File.Exists(destination))
        {
            throw new InvalidOperationException("The repository destination already exists.");
        }

        try
        {
            var result = await _gitCliService.ExecuteBufferedAsync(
                    ["init", $"--initial-branch={branch}", "--", destination],
                    parent,
                    validateExitCode: false,
                    cancellationToken)
                .ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(ErrorMessage(result, "Git init failed."));
            }

            if (!Directory.Exists(Path.Combine(destination, ".git")))
            {
                throw new InvalidDataException("Git init completed without creating a repository.");
            }

            var commit = await _gitCliService
                .CreateCommand(
                    ["commit", "--allow-empty", "--no-gpg-sign", "--message", "Initial commit"],
                    destination,
                    validateExitCode: false,
                    commitEnvironment)
                .ExecuteBufferedAsync(cancellationToken)
                .ConfigureAwait(false);
            if (commit.ExitCode != 0)
            {
                throw new InvalidOperationException(ErrorMessage(
                    commit,
                    "Could not create the initial commit. Configure your Git author name and email, then retry."));
            }

            return destination;
        }
        catch
        {
            TryDeleteDestination(destination);
            throw;
        }
    }

    private static string NormalizeBranchName(string initialBranchName)
    {
        var normalized = initialBranchName.Trim();
        if (normalized.Length is 0 or > 255 || !IsValidBranchName(normalized.AsSpan()))
        {
            throw new ArgumentException("Initial branch name is not valid.", nameof(initialBranchName));
        }

        return normalized;
    }

    private static bool IsValidBranchName(ReadOnlySpan<char> name)
    {
        if (name[0] == '-' || name[^1] is '/' or '.' ||
            name.SequenceEqual("@") || !IsValidComponent(name[..ComponentEnd(name)]))
        {
            return false;
        }

        for (var index = 0; index < name.Length; index++)
        {
            var character = name[index];
            if (character <= ' ' || character == '' || character is '~' or '^' or ':' or '?' or '*' or '[' or '\\')
            {
                return false;
            }

            if (index > 0 &&
                (character == '.' && name[index - 1] == '.' ||
                 character == '{' && name[index - 1] == '@'))
            {
                return false;
            }

            if (character == '/')
            {
                var remaining = name[(index + 1)..];
                if (!IsValidComponent(remaining[..ComponentEnd(remaining)]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static int ComponentEnd(ReadOnlySpan<char> name)
    {
        var slash = name.IndexOf('/');
        return slash >= 0 ? slash : name.Length;
    }

    private static bool IsValidComponent(ReadOnlySpan<char> component) =>
        component.Length > 0 &&
        component[0] != '.' &&
        !component.EndsWith(".lock", StringComparison.Ordinal);

    private static string NormalizeParentPath(string parentPath)
    {
        var candidate = parentPath.Trim();
        if (candidate.Length == 0)
        {
            throw new ArgumentException("Destination folder is required.", nameof(parentPath));
        }

        var normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
        return Directory.Exists(normalized)
            ? normalized
            : throw new DirectoryNotFoundException("The destination folder does not exist.");
    }

    private static string NormalizeDirectoryName(string directoryName)
    {
        var normalized = directoryName.Trim();
        if (normalized.Length is 0 or > 255 ||
            normalized is "." or ".." ||
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
        var destination = Path.GetFullPath(Path.Combine(parentPath, directoryName));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(Path.GetDirectoryName(destination), parentPath, comparison)
            ? destination
            : throw new ArgumentException("Repository destination must be inside the selected folder.");
    }

    private static string ErrorMessage(BufferedCommandResult result, string fallback)
    {
        var error = result.StandardError.Trim();
        return error.Length > 0 ? error : fallback;
    }

    private static void TryDeleteDestination(string destination)
    {
        try
        {
            if (!Directory.Exists(destination))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            foreach (var directory in Directory.EnumerateDirectories(destination, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(directory, FileAttributes.Normal);
            }

            File.SetAttributes(destination, FileAttributes.Normal);
            Directory.Delete(destination, recursive: true);
        }
        catch
        {
        }
    }
}
