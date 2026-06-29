namespace LovelyGit.DiffBenchmarks;

internal static class ChromiumBenchmarkFixtures
{
    private static readonly (string Name, string Path)[] Files =
    [
        ("chromium-json-manifest-edit", @"third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json"),
        ("chromium-xml-cdata-edit", @"third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml"),
        ("chromium-luci-cfg-edit", @"infra\config\generated\luci\cr-buildbucket.cfg"),
        ("chromium-header-normalization-edit", @"third_party\sentencepiece\src\src\normalization_rule.h"),
        ("chromium-cpp-simdutf-edit", @"third_party\simdutf\simdutf.cpp"),
        ("chromium-gn-xnnpack-edit", @"third_party\xnnpack\BUILD.gn"),
        ("chromium-actions-xml-edit", @"tools\metrics\actions\actions.xml"),
        ("chromium-js-pdf-worker-edit", @"third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js"),
    ];

    public static IReadOnlyList<BenchmarkCase> Create(string chromiumRepoPath)
    {
        if (!Directory.Exists(chromiumRepoPath))
        {
            return [];
        }

        var cases = new List<BenchmarkCase>();
        foreach (var file in Files)
        {
            var path = Path.Combine(chromiumRepoPath, file.Path);
            if (!File.Exists(path))
            {
                continue;
            }

            var oldLines = File.ReadAllLines(path);
            if (oldLines.Length < 4)
            {
                continue;
            }

            var newLines = Mutate(oldLines, file.Name);
            cases.Add(new BenchmarkCase(
                file.Name,
                oldLines.Length,
                string.Join('\n', oldLines),
                string.Join('\n', newLines),
                $"real Chromium file: {file.Path}"));
        }

        return cases;
    }

    private static string[] Mutate(string[] oldLines, string name)
    {
        var lines = oldLines.ToList();
        var top = Math.Min(5, lines.Count - 1);
        var middle = lines.Count / 2;
        var bottom = Math.Max(0, lines.Count - 8);
        lines[top] = lines[top] + " // LovelyGit benchmark edit";
        lines.Insert(middle, $"// LovelyGit benchmark inserted marker for {name}");
        lines.RemoveAt(Math.Min(bottom, lines.Count - 1));
        return lines.ToArray();
    }
}
