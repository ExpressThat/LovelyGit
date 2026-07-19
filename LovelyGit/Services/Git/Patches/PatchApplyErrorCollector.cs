namespace ExpressThat.LovelyGit.Services.Git.Patches;

internal sealed class PatchApplyErrorCollector
{
    private const int MaxRetainedLineLength = 512;
    private string? _first;
    private string? _second;
    private bool _hasAdditional;

    public void Add(string line)
    {
        var message = line.Trim();
        if (message.Length == 0) return;
        if (message.Length > MaxRetainedLineLength)
        {
            message = string.Concat(message.AsSpan(0, MaxRetainedLineLength - 3), "...");
        }

        if (_first == null) _first = message;
        else if (_second == null) _second = message;
        else _hasAdditional = true;
    }

    public string FormatFailure()
    {
        if (_first == null)
        {
            return "Git could not apply this patch to the current repository state.";
        }

        var message = _second == null ? _first : $"{_first}\n{_second}";
        return _hasAdditional
            ? $"{message}\nAdditional patch failures were omitted."
            : message;
    }
}
