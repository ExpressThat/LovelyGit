using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeStatusListService
{
    private async Task<WorkingTreeChangesResponse> GetPorcelainChangesAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var result = await _gitCliService
            .ExecuteBufferedAsync(
                ["--no-optional-locks", "status", "--porcelain=v1", "-z", "--untracked-files=all"],
                paths.WorkTreeDirectory,
                validateExitCode: false,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(FirstNonEmptyLine(result.StandardError)
                ?? FirstNonEmptyLine(result.StandardOutput)
                ?? "Git status failed.");
        }

        return ParsePorcelainStatus(result.StandardOutput.AsSpan());
    }

    internal static WorkingTreeChangesResponse ParsePorcelainStatus(ReadOnlySpan<char> output)
    {
        var response = new WorkingTreeChangesResponse();
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
            var path = ReadNulTerminated(output, ref offset);
            var oldPath = x is 'R' or 'C' || y is 'R' or 'C'
                ? ReadNulTerminated(output, ref offset)
                : null;
            AddStatus(response, x, y, path, oldPath);
        }

        Sort(response.Staged);
        Sort(response.Unstaged);
        Sort(response.Untracked);
        Sort(response.Unmerged);
        return response;
    }

    private static void AddStatus(
        WorkingTreeChangesResponse response,
        char x,
        char y,
        string path,
        string? oldPath)
    {
        if (x == '?' && y == '?')
        {
            response.Untracked.Add(Create(path, oldPath, "Added", WorkingTreeChangeGroup.Untracked));
            return;
        }

        if (IsUnmerged(x, y))
        {
            response.Unmerged.Add(Create(path, oldPath, "Unmerged", WorkingTreeChangeGroup.Unmerged));
            return;
        }

        if (x != ' ')
        {
            response.Staged.Add(Create(path, oldPath, ToStatus(x), WorkingTreeChangeGroup.Staged));
        }

        if (y != ' ')
        {
            response.Unstaged.Add(Create(path, oldPath, ToStatus(y), WorkingTreeChangeGroup.Unstaged));
        }
    }

    private static string ReadNulTerminated(ReadOnlySpan<char> output, ref int offset)
    {
        var remaining = output[offset..];
        var nulIndex = remaining.IndexOf('\0');
        if (nulIndex < 0)
        {
            offset = output.Length;
            return remaining.ToString();
        }

        var value = remaining[..nulIndex].ToString();
        offset += nulIndex + 1;
        return value;
    }

    private static bool IsUnmerged(char x, char y) =>
        x == 'U' || y == 'U' || (x == 'A' && y == 'A') || (x == 'D' && y == 'D');

    private static string ToStatus(char value) =>
        value switch
        {
            'A' => "Added",
            'D' => "Deleted",
            'R' => "Renamed",
            'C' => "Copied",
            _ => "Modified",
        };

    private static string? FirstNonEmptyLine(string text)
    {
        foreach (var line in text.AsSpan().EnumerateLines())
        {
            var trimmed = line.Trim();
            if (!trimmed.IsEmpty)
            {
                return trimmed.ToString();
            }
        }

        return null;
    }
}
