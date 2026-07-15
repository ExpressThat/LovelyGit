using System.Buffers;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

internal static class GitPrimaryRemoteUrlReader
{
    private const int BufferSize = 16 * 1024;

    public static async Task<string?> ReadAsync(
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(gitDirectory, "config");
        if (!File.Exists(path)) return null;
        var state = new ParseState();
        await ReadLinesAsync(path, state, cancellationToken).ConfigureAwait(false);
        return state.OriginUrl ?? state.FallbackUrl;
    }

    private static async Task ReadLinesAsync(
        string path,
        ParseState state,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<char>.Shared.Rent(BufferSize);
        StringBuilder? pending = null;
        try
        {
            await using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
                BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var reader = new StreamReader(stream);
            int read;
            while ((read = await reader.ReadAsync(
                       buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false)) > 0)
            {
                var remaining = buffer.AsSpan(0, read);
                while (true)
                {
                    var newline = remaining.IndexOf('\n');
                    if (newline < 0) break;
                    ProcessSegment(remaining[..newline], ref pending, state);
                    cancellationToken.ThrowIfCancellationRequested();
                    remaining = remaining[(newline + 1)..];
                }
                if (!remaining.IsEmpty)
                {
                    pending ??= new StringBuilder(remaining.Length + 128);
                    pending.Append(remaining);
                }
            }
            if (pending is { Length: > 0 }) state.ProcessLine(pending.ToString());
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    private static void ProcessSegment(
        ReadOnlySpan<char> segment,
        ref StringBuilder? pending,
        ParseState state)
    {
        if (pending == null)
        {
            state.ProcessLine(segment);
            return;
        }
        pending.Append(segment);
        state.ProcessLine(pending.ToString());
        pending = null;
    }

    private sealed class ParseState
    {
        private bool _inRemote;
        private bool _inOrigin;
        public string? OriginUrl { get; private set; }
        public string? FallbackUrl { get; private set; }

        public void ProcessLine(ReadOnlySpan<char> raw)
        {
            var line = raw.Trim();
            if (line.IsEmpty || line[0] is '#' or ';') return;
            if (GitRemoteConfigReader.TryReadRemoteSectionName(line, out var name))
            {
                _inRemote = true;
                _inOrigin = name.Equals("origin", StringComparison.Ordinal);
                return;
            }
            if (line[0] == '[' && line[^1] == ']')
            {
                _inRemote = false;
                _inOrigin = false;
                return;
            }
            if (!_inRemote || (!_inOrigin && FallbackUrl != null)) return;
            if (!GitRemoteConfigReader.TryReadConfigValue(line, "url", out var url)) return;
            if (_inOrigin) OriginUrl = url;
            else FallbackUrl ??= url;
        }
    }
}
