using System.Security.Cryptography;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionService
{
    private static async Task<string> FingerprintAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return "missing";
        }

        await using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }

    private static async Task<string> ConflictFingerprintAsync(
        string path,
        IReadOnlyDictionary<int, GitIndexEntry> entries,
        CancellationToken cancellationToken)
    {
        var worktreeFingerprint = await FingerprintAsync(path, cancellationToken).ConfigureAwait(false);
        var descriptor = string.Join(
            ';',
            entries.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}:{entry.Value.ObjectId}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes($"{worktreeFingerprint}|{descriptor}")));
    }
}
