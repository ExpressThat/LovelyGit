using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class WorkingTreeSummaryService
{
    private readonly WorkingTreeStatusListService _statusListService;

    public WorkingTreeSummaryService(WorkingTreeStatusListService statusListService)
    {
        _statusListService = statusListService;
    }

    public async Task<WorkingTreeChangeSummaryResponse> GetSummaryAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
        => await _statusListService.GetSummaryAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

    internal static int CountPorcelainRecords(ReadOnlySpan<char> output)
    {
        var count = 0;
        var offset = 0;
        while (offset < output.Length)
        {
            if (output.Length - offset < 4)
            {
                break;
            }

            var x = output[offset];
            var y = output[offset + 1];
            offset += 3;
            offset = AdvancePastNul(output, offset);
            if (x is 'R' or 'C' || y is 'R' or 'C')
            {
                offset = AdvancePastNul(output, offset);
            }

            count++;
        }

        return count;
    }

    private static int AdvancePastNul(ReadOnlySpan<char> output, int offset)
    {
        var remaining = output[offset..];
        var nulIndex = remaining.IndexOf('\0');
        return nulIndex < 0 ? output.Length : offset + nulIndex + 1;
    }
}
