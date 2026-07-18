using System.Text;

namespace LovelyGit.Tests.Git;

internal static class PackedRefFixture
{
    private const int LooseRefsPerKind = 8;

    public static void AddBranches(
        string repositoryPath,
        string commit,
        int count,
        string prefix = "perf/branch-")
    {
        var references = Enumerable.Range(0, count)
            .Select(index => $"refs/heads/{prefix}{index:D4}");
        AddMixed(repositoryPath, commit, references);
    }

    public static void AddBranchRemoteTagSets(
        string repositoryPath,
        string commit,
        int countPerKind)
    {
        var references = new List<string>(countPerKind * 3);
        for (var index = 0; index < countPerKind; index++)
        {
            references.Add($"refs/heads/branch-{index}");
            references.Add($"refs/remotes/origin/branch-{index}");
            references.Add($"refs/tags/tag-{index}");
        }
        AddMixed(repositoryPath, commit, references);
    }

    private static void AddMixed(
        string repositoryPath,
        string commit,
        IEnumerable<string> references)
    {
        var gitDirectory = Path.Combine(repositoryPath, ".git");
        var packed = new List<string>();
        var looseByKind = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var reference in references)
        {
            var kind = reference.Split('/', 3)[1];
            looseByKind.TryGetValue(kind, out var looseCount);
            if (looseCount < LooseRefsPerKind)
            {
                WriteLooseRef(gitDirectory, reference, commit);
                looseByKind[kind] = looseCount + 1;
            }
            else
            {
                packed.Add(reference);
            }
        }
        AppendPackedRefs(gitDirectory, commit, packed);
    }

    private static void AppendPackedRefs(
        string gitDirectory,
        string commit,
        IEnumerable<string> references)
    {
        var path = Path.Combine(gitDirectory, "packed-refs");
        var entries = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (File.Exists(path))
        {
            foreach (var line in File.ReadLines(path))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                if (line[0] == '^') throw new InvalidOperationException(
                    "PackedRefFixture does not support peeled fixture refs.");
                var separator = line.IndexOf(' ');
                if (separator > 0) entries[line[(separator + 1)..]] = line[..separator];
            }
        }
        foreach (var reference in references) entries[reference] = commit;

        var contents = new StringBuilder(entries.Count * 80)
            .AppendLine("# pack-refs with: sorted");
        foreach (var (reference, hash) in entries)
            contents.Append(hash).Append(' ').AppendLine(reference);
        File.WriteAllText(path, contents.ToString().ReplaceLineEndings("\n"));
    }

    private static void WriteLooseRef(string gitDirectory, string name, string hash)
    {
        var path = Path.Combine(gitDirectory, name.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, hash + "\n");
    }
}
