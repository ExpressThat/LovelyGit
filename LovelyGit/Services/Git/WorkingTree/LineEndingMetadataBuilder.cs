using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class LineEndingMetadataBuilder
{
    private const string CrLf = "\r\n";
    private const string Lf = "\n";
    private const string Cr = "\r";
    private const string None = "";

    public static CommitFileDiffResponse Apply(
        CommitFileDiffResponse response,
        ReadOnlySpan<byte> oldBytes,
        ReadOnlySpan<byte> newBytes)
    {
        var oldProfile = Analyze(oldBytes);
        var newProfile = Analyze(newBytes);
        response.OldLineEnding = oldProfile.Default;
        response.NewLineEnding = newProfile.Default;
        response.OldLineEndingOverrides = oldProfile.Overrides;
        response.NewLineEndingOverrides = newProfile.Overrides;
        return response;
    }

    internal static LineEndingProfile Analyze(ReadOnlySpan<byte> bytes)
    {
        Span<int> counts = stackalloc int[4];
        Count(bytes, counts);
        var defaultKind = MostFrequent(counts);
        var overrides = counts[defaultKind] == counts[0] + counts[1] + counts[2] + counts[3]
            ? []
            : BuildOverrides(bytes, defaultKind);
        return new LineEndingProfile(Ending(defaultKind), overrides);
    }

    private static void Count(ReadOnlySpan<byte> bytes, Span<int> counts)
    {
        var lineStart = 0;
        for (var index = 0; index < bytes.Length; index++)
        {
            var kind = KindAt(bytes, ref index);
            if (kind < 0) continue;
            counts[kind]++;
            lineStart = index + 1;
        }

        if (lineStart < bytes.Length) counts[3]++;
    }

    private static List<int> BuildOverrides(
        ReadOnlySpan<byte> bytes,
        int defaultKind)
    {
        var overrides = new List<int>();
        var lineNumber = 1;
        var lineStart = 0;
        for (var index = 0; index < bytes.Length; index++)
        {
            var kind = KindAt(bytes, ref index);
            if (kind < 0) continue;
            AddOverride(overrides, lineNumber++, kind, defaultKind);
            lineStart = index + 1;
        }

        if (lineStart < bytes.Length) AddOverride(overrides, lineNumber, 3, defaultKind);
        return overrides;
    }

    private static int KindAt(ReadOnlySpan<byte> bytes, ref int index)
    {
        if (bytes[index] == (byte)'\r')
        {
            if (index + 1 < bytes.Length && bytes[index + 1] == (byte)'\n')
            {
                index++;
                return 0;
            }

            return 2;
        }

        return bytes[index] == (byte)'\n' ? 1 : -1;
    }

    private static int MostFrequent(ReadOnlySpan<int> counts)
    {
        if (counts[0] + counts[1] + counts[2] + counts[3] == 0) return 1;
        var best = 0;
        for (var index = 1; index < counts.Length; index++)
            if (counts[index] > counts[best]) best = index;
        return best;
    }

    private static void AddOverride(
        List<int> overrides,
        int lineNumber,
        int kind,
        int defaultKind)
    {
        if (kind == defaultKind) return;
        overrides.Add(checked((lineNumber * 4) + kind));
    }

    private static string Ending(int kind) => kind switch
    {
        0 => CrLf,
        1 => Lf,
        2 => Cr,
        _ => None,
    };
}

internal readonly record struct LineEndingProfile(
    string Default,
    List<int> Overrides);
