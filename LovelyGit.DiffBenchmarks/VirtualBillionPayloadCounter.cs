using System.Text;

namespace LovelyGit.DiffBenchmarks;

internal static class VirtualBillionPayloadCounter
{
    private const string LineText = "virtual billion row";

    public static long Count(CommitFileDiffResponse response)
    {
        var count = (long)Encoding.UTF8.GetByteCount(Header(response));
        count += RowBytes(VirtualBillionBenchmarkFixtures.LineCount);
        return count + 2;
    }

    private static string Header(CommitFileDiffResponse response)
    {
        return "{\"commitHash\":\"" + response.CommitHash
            + "\",\"path\":\"" + response.Path
            + "\",\"status\":\"" + response.Status
            + "\",\"viewMode\":\"" + response.ViewMode
            + "\",\"isBinary\":false,\"hasDifferences\":false,\"isTruncated\":false,"
            + "\"truncationMessage\":\"\",\"lines\":[";
    }

    private static long RowBytes(int rows)
    {
        var rowConstant = "{\"oldLineNumber\":".Length
            + ",\"newLineNumber\":".Length
            + ",\"text\":\"".Length
            + LineText.Length
            + "\",\"changeType\":\"Unchanged\"}".Length;
        return (long)rows * rowConstant
            + 2L * SumDigits(rows)
            + rows - 1L;
    }

    private static long SumDigits(int rows)
    {
        var sum = 0L;
        var start = 1L;
        for (var digits = 1; start <= rows; digits++)
        {
            var end = Math.Min(rows, start * 10 - 1);
            sum += (end - start + 1) * digits;
            start *= 10;
        }

        return sum;
    }
}
