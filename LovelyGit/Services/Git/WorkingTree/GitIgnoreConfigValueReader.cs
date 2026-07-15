using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class GitIgnoreConfigValueReader
{
    public static async Task<string?> ReadAsync(
        string path,
        string sectionName,
        string keyName,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return null;
        var state = new ParseState(sectionName, keyName);
        await PooledTextLineReader.ReadAsync(
                path,
                state,
                static (line, parseState) => parseState.ProcessLine(line),
                cancellationToken)
            .ConfigureAwait(false);
        return state.Value;
    }

    private sealed class ParseState(string sectionName, string keyName)
    {
        private bool _inSection;
        public string? Value { get; private set; }

        public void ProcessLine(ReadOnlySpan<char> rawLine)
        {
            if (Value != null) return;
            var line = rawLine.Trim();
            if (line.IsEmpty || line[0] is '#' or ';') return;
            if (line[0] == '[' && line[^1] == ']')
            {
                _inSection = line[1..^1].Trim().Trim('"')
                    .Equals(sectionName, StringComparison.OrdinalIgnoreCase);
                return;
            }
            if (!_inSection) return;
            var separator = line.IndexOf('=');
            if (separator <= 0 || !line[..separator].Trim()
                    .Equals(keyName, StringComparison.OrdinalIgnoreCase)) return;
            Value = line[(separator + 1)..].Trim().Trim('"').ToString();
        }
    }
}
