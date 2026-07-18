using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Submodules;

internal static class SubmoduleRepositoryTemplate
{
    private static readonly RepositoryTemplate<string> Template = new(
        "lovelygit-submodules-template-",
        Initialize);

    public static DirectoryInfo CreateCopy(string prefix)
    {
        var (root, templateRoot) = Template.CreateCopy(prefix);
        RewriteCopiedPaths(root, templateRoot);
        return root;
    }

    private static string Initialize(DirectoryInfo root)
    {
        var childPath = Path.Combine(root.FullName, "child");
        var parentPath = Path.Combine(root.FullName, "parent");
        Directory.CreateDirectory(childPath);
        Directory.CreateDirectory(parentPath);
        InitializeRepository(childPath);
        File.WriteAllText(Path.Combine(childPath, "library.txt"), "library\n");
        RunGit(childPath, ["add", "."]);
        RunGit(childPath, ["commit", "-m", "Library"]);
        InitializeRepository(parentPath);
        RunGit(parentPath,
        [
            "-c", "protocol.file.allow=always", "submodule", "add", "--name", "library",
            childPath, "deps/library",
        ]);
        RunGit(parentPath, ["commit", "-am", "Add submodule"]);
        return root.FullName;
    }

    private static void InitializeRepository(string path)
    {
        InitializedRepositoryTemplate.CopyInto(new DirectoryInfo(path), "master");
        RunGit(path, ["config", "protocol.file.allow", "always"]);
    }

    private static void RewriteCopiedPaths(DirectoryInfo root, string templateRoot)
    {
        foreach (var path in RewrittenPaths(root))
        {
            if (!File.Exists(path)) continue;
            var content = File.ReadAllText(path);
            var attributes = File.GetAttributes(path);
            var rewritten = content
                .Replace(templateRoot, root.FullName, StringComparison.OrdinalIgnoreCase)
                .Replace(
                    templateRoot.Replace('\\', '/'),
                    root.FullName.Replace('\\', '/'),
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    templateRoot.Replace("\\", "\\\\", StringComparison.Ordinal),
                    root.FullName.Replace("\\", "\\\\", StringComparison.Ordinal),
                    StringComparison.OrdinalIgnoreCase);
            File.SetAttributes(path, FileAttributes.Normal);
            File.WriteAllText(path, rewritten);
            File.SetAttributes(path, attributes);
        }
    }

    private static IEnumerable<string> RewrittenPaths(DirectoryInfo root)
    {
        var parent = Path.Combine(root.FullName, "parent");
        yield return Path.Combine(parent, ".gitmodules");
        yield return Path.Combine(parent, ".git", "config");
        yield return Path.Combine(parent, ".git", "modules", "deps", "library", "config");
        yield return Path.Combine(parent, "deps", "library", ".git");
    }

    private static void RunGit(string path, IReadOnlyList<string> arguments) =>
        new GitCliService().ExecuteBufferedAsync(arguments, path).GetAwaiter().GetResult();
}
