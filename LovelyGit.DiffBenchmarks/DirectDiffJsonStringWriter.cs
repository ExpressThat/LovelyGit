namespace LovelyGit.DiffBenchmarks;

internal static class DirectDiffJsonStringWriter
{
    public static string Write(CommitFileDiffResponse response)
    {
        var length = DirectDiffJsonLengthCounter.Count(response);
        return string.Create(length, response, static (span, state) =>
        {
            var writer = new DirectDiffJsonSpanWriter(span);
            writer.Response(state);
        });
    }
}
