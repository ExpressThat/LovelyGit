using LibGit2Sharp;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExpressThat.LovelyGit.Services.Git
{
    public class CommitGraph
    {
        private const int CommitMessagePreviewChars = 160;
        private const int CheckpointIntervalRows = 400;
        private const int MaxCheckpointCount = 5000;

        private readonly RepositoryManager _repositoryManager;
        private readonly object _cacheLock = new();
        private CommitGraphMetadataCache? _metadataCache;
        private readonly Dictionary<string, CommitGraphCheckpointCache> _checkpointCaches = new();

        public CommitGraph(RepositoryManager repositoryManager)
        {
            _repositoryManager = repositoryManager;
        }

        public async Task PrimeMetadataAsync()
        {
            try
            {
                var repository = _repositoryManager.GetRepository();
                var cacheKey = CreateMetadataCacheKey(repository);
                var repoPath = repository.Info.Path;
                var commitFilter = CreateCommitFilter(repository);

                await EnsureMetadataCacheAsync(cacheKey, repoPath, commitFilter, CancellationToken.None);
                EnsureCheckpointCache(cacheKey);
                await WarmGraphPathAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CommitGraph priming failed: {ex.Message}");
            }
        }

        private async Task WarmGraphPathAsync()
        {
            try
            {
                var repository = _repositoryManager.GetRepository();
                var commitFilter = CreateCommitFilter(repository);

                foreach (var commit in repository.Commits.QueryBy(commitFilter))
                {
                    _ = commit.Sha;
                    _ = commit.MessageShort;
                    _ = commit.Parents.FirstOrDefault()?.Sha;
                    break;
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CommitGraph graph-path warmup failed: {ex.Message}");
            }
        }

        public Task<CommitGraphResponse> GetCommitGraphAsync(int offset, int limit, CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
            {
                if (offset < 0) offset = 0;
                if (limit < 0) limit = 0;

                var repository = _repositoryManager.GetRepository();
                var repoPath = repository.Info.Path;
                var commitFilter = CreateCommitFilter(repository);
                var cacheKey = CreateMetadataCacheKey(repository);

                var metadata = await EnsureMetadataCacheAsync(cacheKey, repoPath, commitFilter, cancellationToken);
                var checkpointCache = EnsureCheckpointCache(cacheKey);

                return BuildGraphWindowWithCheckpoints(
                    repository,
                    commitFilter,
                    metadata.BranchesMap,
                    metadata.TagsMap,
                    metadata.TotalRows,
                    checkpointCache,
                    offset,
                    limit,
                    cancellationToken);
            }, cancellationToken);
        }

        public Task<CommitGraphResponse> GetCommitGraph(int offset, int limit, CancellationToken cancellationToken = default)
        {
            return GetCommitGraphAsync(offset, limit, cancellationToken);
        }

        private CommitGraphResponse BuildGraphWindowWithCheckpoints(
            Repository repository,
            CommitFilter commitFilter,
            Dictionary<string, List<string>> branchesMap,
            Dictionary<string, List<string>> tagsMap,
            int totalRows,
            CommitGraphCheckpointCache checkpointCache,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            var totalStopwatch = Stopwatch.StartNew();
            var end = offset + limit;
            if (limit == 0)
            {
                return new CommitGraphResponse
                {
                    TotalRows = totalRows,
                    LaneCount = 0,
                    Rows = new List<CommitGraphRow>(),
                };
            }

            var lockStopwatch = Stopwatch.StartNew();
            var startCheckpoint = checkpointCache.GetNearestCheckpointAtOrBefore(offset);
            var lockReadMs = lockStopwatch.ElapsedMilliseconds;

            var workStopwatch = Stopwatch.StartNew();
            var activeLaneTargets = new List<string?>(startCheckpoint.ActiveLaneTargets);
            var maxLaneCount = startCheckpoint.MaxLaneCount;
            var rows = new List<CommitGraphRow>(limit);
            var rowIndex = startCheckpoint.RowIndex;
            var pendingCheckpoints = new List<CommitGraphCheckpoint>();

            var commits = repository.Commits.QueryBy(commitFilter).Skip(startCheckpoint.RowIndex);
            var replayedRows = 0;

            foreach (var commit in commits)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (rowIndex >= end)
                {
                    break;
                }

                if (rowIndex >= 0 && rowIndex % CheckpointIntervalRows == 0)
                {
                    pendingCheckpoints.Add(new CommitGraphCheckpoint
                    {
                        RowIndex = rowIndex,
                        ActiveLaneTargets = new List<string?>(activeLaneTargets),
                        MaxLaneCount = maxLaneCount,
                    });
                }

                var isVisibleRow = rowIndex >= offset && rows.Count < limit;
                var incomingLanes = FindAllLanesByTarget(activeLaneTargets, commit.Sha);
                var activeLanesAbove = isVisibleRow ? GetActiveLanes(activeLaneTargets) : null;
                var currentLane = incomingLanes.Count > 0 ? incomingLanes[0] : AllocateLane(activeLaneTargets);

                foreach (var lane in incomingLanes)
                {
                    activeLaneTargets[lane] = null;
                }

                string? mainParent = null;
                List<string>? mergeParents = null;
                var parentCount = 0;
                foreach (var parent in commit.Parents)
                {
                    if (parentCount == 0)
                    {
                        mainParent = parent.Sha;
                    }
                    else
                    {
                        mergeParents ??= new List<string>();
                        mergeParents.Add(parent.Sha);
                    }
                    parentCount++;
                }

                if (!string.IsNullOrEmpty(mainParent))
                {
                    SetLaneTarget(activeLaneTargets, currentLane, mainParent);
                }
                else if (currentLane < activeLaneTargets.Count)
                {
                    activeLaneTargets[currentLane] = null;
                }

                List<int>? mergeParentLanes = null;
                if (mergeParents != null)
                {
                    foreach (var parent in mergeParents)
                    {
                        var parentLane = FindLaneByTarget(activeLaneTargets, parent) ?? AllocateLane(activeLaneTargets);
                        SetLaneTarget(activeLaneTargets, parentLane, parent);
                        if (isVisibleRow)
                        {
                            mergeParentLanes ??= new List<int>();
                            mergeParentLanes.Add(parentLane);
                        }
                    }
                }

                TrimTrailingEmptyLanes(activeLaneTargets);
                maxLaneCount = System.Math.Max(maxLaneCount, activeLaneTargets.Count);

                if (isVisibleRow)
                {
                    var commitInfo = BuildCommitInfo(commit, branchesMap, tagsMap);
                    var activeLanesBelow = GetActiveLanes(activeLaneTargets);
                    var edgesAbove = incomingLanes
                        .Select(lane => new CommitLaneEdge
                        {
                            FromLane = lane,
                            ToLane = currentLane,
                            Kind = lane == currentLane ? "straight" : "merge_in",
                        })
                        .ToList();

                    var edgesBelow = new List<CommitLaneEdge>();
                    if (!string.IsNullOrEmpty(mainParent))
                    {
                        edgesBelow.Add(new CommitLaneEdge
                        {
                            FromLane = currentLane,
                            ToLane = currentLane,
                            Kind = "straight",
                        });
                    }
                    if (mergeParentLanes != null)
                    {
                        foreach (var parentLane in mergeParentLanes)
                        {
                            edgesBelow.Add(new CommitLaneEdge
                            {
                                FromLane = currentLane,
                                ToLane = parentLane,
                                Kind = "merge_in",
                            });
                        }
                    }

                    rows.Add(new CommitGraphRow
                    {
                        Commit = commitInfo,
                        RowIndex = rowIndex,
                        Lane = currentLane,
                        ActiveLanes = activeLanesBelow.ToList(),
                        ActiveLanesAbove = activeLanesAbove ?? new List<int>(),
                        ActiveLanesBelow = activeLanesBelow,
                        EdgesAbove = edgesAbove,
                        EdgesBelow = edgesBelow,
                        IsMergeCommit = parentCount > 1,
                        IsBranchTip = commitInfo.Branches.Count > 0,
                    });
                }

                rowIndex++;
                replayedRows++;
            }

            pendingCheckpoints.Add(new CommitGraphCheckpoint
            {
                RowIndex = rowIndex,
                ActiveLaneTargets = new List<string?>(activeLaneTargets),
                MaxLaneCount = maxLaneCount,
            });

            var workMs = workStopwatch.ElapsedMilliseconds;

            var lockWriteStopwatch = Stopwatch.StartNew();
            checkpointCache.MergeCheckpoints(pendingCheckpoints);
            var lockWriteMs = lockWriteStopwatch.ElapsedMilliseconds;

            var response = new CommitGraphResponse
            {
                TotalRows = totalRows,
                LaneCount = maxLaneCount,
                Rows = rows,
            };

            var totalMs = totalStopwatch.ElapsedMilliseconds;
            Console.WriteLine(
                $"[CommitGraph] offset={offset} limit={limit} checkpoint={startCheckpoint.RowIndex} replayed={replayedRows} lock_read_ms={lockReadMs} work_ms={workMs} lock_write_ms={lockWriteMs} total_ms={totalMs}");

            return response;
        }

        private async Task<CommitGraphMetadataCache> EnsureMetadataCacheAsync(
            string cacheKey,
            string repoPath,
            CommitFilter commitFilter,
            CancellationToken cancellationToken)
        {
            lock (_cacheLock)
            {
                if (_metadataCache != null && _metadataCache.Key == cacheKey)
                {
                    return _metadataCache;
                }
            }

            var branchesTask = Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var repo = new Repository(repoPath);
                return BuildBranchesMap(repo);
            }, cancellationToken);

            var tagsTask = Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var repo = new Repository(repoPath);
                return BuildTagsMap(repo);
            }, cancellationToken);

            var totalRowsTask = Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var repo = new Repository(repoPath);
                return repo.Commits.QueryBy(commitFilter).Count();
            }, cancellationToken);

            await Task.WhenAll(branchesTask, tagsTask, totalRowsTask);

            var cache = new CommitGraphMetadataCache
            {
                Key = cacheKey,
                BranchesMap = branchesTask.Result,
                TagsMap = tagsTask.Result,
                TotalRows = totalRowsTask.Result,
            };

            lock (_cacheLock)
            {
                // Re-check under lock in case another request already populated this key.
                if (_metadataCache != null && _metadataCache.Key == cacheKey)
                {
                    return _metadataCache;
                }

                _metadataCache = cache;

                // Metadata key changes imply refs moved; old checkpoints are stale.
                _checkpointCaches.Clear();
            }

            return cache;
        }

        private CommitGraphCheckpointCache EnsureCheckpointCache(string cacheKey)
        {
            lock (_cacheLock)
            {
                if (_checkpointCaches.TryGetValue(cacheKey, out var existing))
                {
                    return existing;
                }

                var cache = new CommitGraphCheckpointCache(cacheKey, CheckpointIntervalRows, MaxCheckpointCount);
                _checkpointCaches[cacheKey] = cache;
                return cache;
            }
        }

        private static CommitFilter CreateCommitFilter(Repository repository)
        {
            var includeReachableFrom = repository.Refs
                .Where(r =>
                    r.CanonicalName.StartsWith("refs/heads/") ||
                    r.CanonicalName.StartsWith("refs/remotes/") ||
                    r.CanonicalName.StartsWith("refs/tags/"))
                .Select(r => r.TargetIdentifier)
                .ToList();

            if (!includeReachableFrom.Any())
            {
                includeReachableFrom.Add("HEAD");
            }

            return new CommitFilter
            {
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time,
                IncludeReachableFrom = includeReachableFrom,
            };
        }

        private static string CreateMetadataCacheKey(Repository repository)
        {
            var refParts = repository.Refs
                .Where(r =>
                    r.CanonicalName.StartsWith("refs/heads/") ||
                    r.CanonicalName.StartsWith("refs/remotes/") ||
                    r.CanonicalName.StartsWith("refs/tags/"))
                .Select(r => $"{r.CanonicalName}:{r.TargetIdentifier}")
                .OrderBy(v => v);

            return $"{repository.Info.Path}|{string.Join("|", refParts)}";
        }

        private static Dictionary<string, List<string>> BuildBranchesMap(Repository repository)
        {
            var map = new Dictionary<string, List<string>>();
            foreach (var branch in repository.Branches)
            {
                var sha = branch.Tip?.Sha;
                if (string.IsNullOrEmpty(sha)) continue;
                if (!map.TryGetValue(sha, out var list))
                {
                    list = new List<string>();
                    map[sha] = list;
                }

                list.Add(branch.FriendlyName);
            }
            return map;
        }

        private static Dictionary<string, List<string>> BuildTagsMap(Repository repository)
        {
            var map = new Dictionary<string, List<string>>();
            foreach (var tag in repository.Tags)
            {
                var sha = TryGetTagCommitSha(tag);
                if (string.IsNullOrEmpty(sha)) continue;
                if (!map.TryGetValue(sha, out var list))
                {
                    list = new List<string>();
                    map[sha] = list;
                }

                list.Add(tag.FriendlyName);
            }
            return map;
        }

        private static string? TryGetTagCommitSha(Tag tag)
        {
            if (tag.Target is Commit directCommit)
            {
                return directCommit.Sha;
            }

            try
            {
                return tag.Target.Peel<Commit>()?.Sha;
            }
            catch (LibGit2SharpException)
            {
                return null;
            }
        }

        private static CommitInfo BuildCommitInfo(
            Commit commit,
            Dictionary<string, List<string>> branchesMap,
            Dictionary<string, List<string>> tagsMap)
        {
            var message = commit.MessageShort?.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                message = (commit.Message ?? string.Empty).Trim();
            }

            message = TruncateMessage(message ?? string.Empty);

            return new CommitInfo
            {
                Hash = commit.Sha,
                Parents = commit.Parents.Select(p => p.Sha).ToList(),
                Author = commit.Author?.Name ?? "unknown",
                Email = commit.Author?.Email ?? string.Empty,
                Date = (commit.Author?.When ?? default).ToUnixTimeSeconds(),
                Message = message,
                Branches = branchesMap.TryGetValue(commit.Sha, out var branches) ? branches : new List<string>(),
                Tags = tagsMap.TryGetValue(commit.Sha, out var tags) ? tags : new List<string>(),
                Stats = null,
            };
        }

        private static string TruncateMessage(string value)
        {
            return value.Length <= CommitMessagePreviewChars
                ? value
                : value.Substring(0, CommitMessagePreviewChars);
        }

        private static List<int> FindAllLanesByTarget(List<string?> activeLaneTargets, string target)
        {
            var lanes = new List<int>();
            for (var i = 0; i < activeLaneTargets.Count; i++)
            {
                if (activeLaneTargets[i] == target)
                {
                    lanes.Add(i);
                }
            }
            return lanes;
        }

        private static int? FindLaneByTarget(List<string?> activeLaneTargets, string target)
        {
            for (var i = 0; i < activeLaneTargets.Count; i++)
            {
                if (activeLaneTargets[i] == target)
                {
                    return i;
                }
            }
            return null;
        }

        private static int AllocateLane(List<string?> activeLaneTargets)
        {
            for (var i = 0; i < activeLaneTargets.Count; i++)
            {
                if (activeLaneTargets[i] == null)
                {
                    return i;
                }
            }

            activeLaneTargets.Add(null);
            return activeLaneTargets.Count - 1;
        }

        private static void SetLaneTarget(List<string?> activeLaneTargets, int lane, string target)
        {
            while (lane >= activeLaneTargets.Count)
            {
                activeLaneTargets.Add(null);
            }
            activeLaneTargets[lane] = target;
        }

        private static void TrimTrailingEmptyLanes(List<string?> activeLaneTargets)
        {
            while (activeLaneTargets.Count > 0 && activeLaneTargets[^1] == null)
            {
                activeLaneTargets.RemoveAt(activeLaneTargets.Count - 1);
            }
        }

        private static List<int> GetActiveLanes(List<string?> activeLaneTargets)
        {
            var lanes = new List<int>();
            for (var i = 0; i < activeLaneTargets.Count; i++)
            {
                if (activeLaneTargets[i] != null)
                {
                    lanes.Add(i);
                }
            }
            return lanes;
        }
    }

    public record CommitStats
    {
        public uint Additions { get; set; }
        public uint Deletions { get; set; }
    }

    public record CommitInfo
    {
        public string Hash { get; set; } = string.Empty;
        public List<string> Parents { get; set; } = new();
        public string Author { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long Date { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Branches { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public CommitStats? Stats { get; set; }
    }

    public record CommitLaneEdge
    {
        public int FromLane { get; set; }
        public int ToLane { get; set; }
        public string Kind { get; set; } = string.Empty;
    }

    public record CommitGraphRow
    {
        public CommitInfo Commit { get; set; } = new();
        public int RowIndex { get; set; }
        public int Lane { get; set; }
        public List<int> ActiveLanes { get; set; } = new();
        public List<int> ActiveLanesAbove { get; set; } = new();
        public List<int> ActiveLanesBelow { get; set; } = new();
        public List<CommitLaneEdge> EdgesAbove { get; set; } = new();
        public List<CommitLaneEdge> EdgesBelow { get; set; } = new();
        public bool IsMergeCommit { get; set; }
        public bool IsBranchTip { get; set; }
    }

    public record CommitGraphResponse
    {
        public int TotalRows { get; set; }
        public int LaneCount { get; set; }
        public List<CommitGraphRow> Rows { get; set; } = new();
    }

    internal sealed record CommitGraphMetadataCache
    {
        public string Key { get; set; } = string.Empty;
        public Dictionary<string, List<string>> BranchesMap { get; set; } = new();
        public Dictionary<string, List<string>> TagsMap { get; set; } = new();
        public int TotalRows { get; set; }
    }

    internal sealed record CommitGraphCheckpoint
    {
        public int RowIndex { get; set; }
        public List<string?> ActiveLaneTargets { get; set; } = new();
        public int MaxLaneCount { get; set; }
    }

    internal sealed class CommitGraphCheckpointCache
    {
        private readonly int _intervalRows;
        private readonly int _maxCheckpointCount;
        private readonly object _lock = new();
        private readonly SortedDictionary<int, CommitGraphCheckpoint> _checkpoints = new();
        private readonly LinkedList<int> _insertionOrder = new();

        public CommitGraphCheckpointCache(string key, int intervalRows, int maxCheckpointCount)
        {
            Key = key;
            _intervalRows = intervalRows;
            _maxCheckpointCount = maxCheckpointCount;

            _checkpoints[0] = new CommitGraphCheckpoint
            {
                RowIndex = 0,
                ActiveLaneTargets = new List<string?>(),
                MaxLaneCount = 0,
            };
            _insertionOrder.AddLast(0);
        }

        public string Key { get; }

        public CommitGraphCheckpoint GetNearestCheckpointAtOrBefore(int rowIndex)
        {
            lock (_lock)
            {
                var nearest = 0;
                foreach (var key in _checkpoints.Keys)
                {
                    if (key > rowIndex) break;
                    nearest = key;
                }

                var cp = _checkpoints[nearest];
                return new CommitGraphCheckpoint
                {
                    RowIndex = cp.RowIndex,
                    ActiveLaneTargets = new List<string?>(cp.ActiveLaneTargets),
                    MaxLaneCount = cp.MaxLaneCount,
                };
            }
        }

        public void MergeCheckpoints(IEnumerable<CommitGraphCheckpoint> checkpoints)
        {
            lock (_lock)
            {
                foreach (var checkpoint in checkpoints)
                {
                    TryAddCheckpointUnderLock(checkpoint);
                }
            }
        }

        private void TryAddCheckpointUnderLock(CommitGraphCheckpoint checkpoint)
        {
            var rowIndex = checkpoint.RowIndex;
            if (rowIndex < 0 || rowIndex % _intervalRows != 0)
            {
                return;
            }

            if (_checkpoints.ContainsKey(rowIndex))
            {
                return;
            }

            _checkpoints[rowIndex] = new CommitGraphCheckpoint
            {
                RowIndex = checkpoint.RowIndex,
                ActiveLaneTargets = new List<string?>(checkpoint.ActiveLaneTargets),
                MaxLaneCount = checkpoint.MaxLaneCount,
            };
            _insertionOrder.AddLast(rowIndex);

            while (_checkpoints.Count > _maxCheckpointCount)
            {
                if (_insertionOrder.First == null)
                {
                    break;
                }

                var oldest = _insertionOrder.First.Value;
                _insertionOrder.RemoveFirst();
                if (oldest == 0)
                {
                    _insertionOrder.AddLast(0);
                    continue;
                }

                _checkpoints.Remove(oldest);
            }
        }
    }
}
