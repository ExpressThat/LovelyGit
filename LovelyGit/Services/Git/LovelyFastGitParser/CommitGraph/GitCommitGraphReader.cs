namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.CommitGraph;

internal sealed partial class GitCommitGraphReader : IDisposable
{
    private const uint NoParent = 0x70000000;
    private const uint ExtraEdges = 0x80000000;
    private const uint PositionMask = 0x7fffffff;
    private readonly IReadOnlyList<GitCommitGraphLayer> _layers;
    private readonly GitObjectFormat _objectFormat;
    private readonly int _totalCount;
    private readonly Dictionary<GitObjectId, int> _positions = [];
    private readonly Dictionary<int, GitObjectId> _idsByPosition = [];
    private readonly object _gate = new();
    private bool _disabled;

    private GitCommitGraphReader(
        IReadOnlyList<GitCommitGraphLayer> layers,
        GitObjectFormat objectFormat,
        int totalCount)
    {
        _layers = layers;
        _objectFormat = objectFormat;
        _totalCount = totalCount;
    }

    public bool TryRead(
        GitObjectId id,
        CancellationToken cancellationToken,
        out GitCommitAncestryHeader header)
    {
        lock (_gate)
        {
            header = default;
            if (_disabled) return false;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryFindPosition(id, cancellationToken, out var position)) return false;
                var layer = FindLayer(position);
                var data = layer.ReadData(position - layer.BasePosition, _objectFormat);
                header = BuildHeader(layer, data, cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (exception is IOException
                                               or UnauthorizedAccessException
                                               or InvalidDataException
                                               or OverflowException)
            {
                _disabled = true;
                return false;
            }
        }
    }

    private GitCommitAncestryHeader BuildHeader(
        GitCommitGraphLayer layer,
        CommitGraphData data,
        CancellationToken cancellationToken)
    {
        if (data.FirstParent == NoParent)
        {
            if (data.SecondParent != NoParent)
                throw new InvalidDataException("Commit-graph root parent data is invalid.");
            return new GitCommitAncestryHeader(data.TreeHash, default, null, 0, data.CommitUnixSeconds);
        }

        var first = ReadPosition(data.FirstParent, cancellationToken);
        if (data.SecondParent == NoParent)
            return new GitCommitAncestryHeader(data.TreeHash, first, null, 1, data.CommitUnixSeconds);

        if ((data.SecondParent & ExtraEdges) == 0)
        {
            var second = ReadPosition(data.SecondParent, cancellationToken);
            return new GitCommitAncestryHeader(data.TreeHash, first, [second], 2, data.CommitUnixSeconds);
        }

        var parents = new List<GitObjectId>(2);
        var edgeIndex = checked((int)(data.SecondParent & PositionMask));
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var edge = layer.ReadExtraEdge(edgeIndex++);
            parents.Add(ReadPosition(edge & PositionMask, cancellationToken));
            if ((edge & ExtraEdges) != 0) break;
            if (parents.Count >= 64) throw new InvalidDataException("Commit-graph parent list is invalid.");
        }
        return new GitCommitAncestryHeader(
            data.TreeHash,
            first,
            parents.ToArray(),
            parents.Count + 1,
            data.CommitUnixSeconds);
    }

    private GitObjectId ReadPosition(uint rawPosition, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (rawPosition >= _totalCount) throw new InvalidDataException("Commit-graph parent position is invalid.");
        var position = checked((int)rawPosition);
        if (_idsByPosition.TryGetValue(position, out var cached)) return cached;
        var layer = FindLayer(position);
        var id = layer.ReadId(position - layer.BasePosition, _objectFormat);
        _positions.TryAdd(id, position);
        _idsByPosition.Add(position, id);
        return id;
    }

    private bool TryFindPosition(
        GitObjectId id,
        CancellationToken cancellationToken,
        out int position)
    {
        if (_positions.TryGetValue(id, out position)) return true;
        for (var index = _layers.Count - 1; index >= 0; index--)
        {
            var local = _layers[index].FindLocalPosition(id, cancellationToken);
            if (local is not { } value) continue;
            position = checked(_layers[index].BasePosition + value);
            _positions.Add(id, position);
            _idsByPosition.TryAdd(position, id);
            return true;
        }
        return false;
    }

    private GitCommitGraphLayer FindLayer(int position)
    {
        for (var index = _layers.Count - 1; index >= 0; index--)
        {
            if (position >= _layers[index].BasePosition) return _layers[index];
        }
        throw new InvalidDataException("Commit-graph position has no layer.");
    }

    public void Dispose()
    {
        foreach (var layer in _layers) layer.Dispose();
        _positions.Clear();
        _idsByPosition.Clear();
    }
}
