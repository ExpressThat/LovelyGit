namespace LovelyGit.DiffBenchmarks;

internal static class DirectDiffJsonUtf8BytesWriter
{
    public static byte[] Write(CommitFileDiffResponse response)
    {
        var output = new byte[DirectDiffJsonUtf8LengthCounter.Count(response)];
        var writer = new DirectDiffJsonUtf8SpanWriter(output, response.Plan!.IsAscii);
        writer.Response(response);
        return output;
    }
}
