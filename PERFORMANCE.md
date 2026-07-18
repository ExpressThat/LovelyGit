# LovelyGit Performance Ledger

This is the durable index of LovelyGit performance work. Update the linked ledgers in the same commit as every performance change. Record the user-visible path, the optimization, evidence, memory effect, and commit. Keep rejected experiments so future work does not repeat them.

## Detailed Ledgers

- [Verified performance results](docs/performance/verified-results.md)
- [Completed optimization inventory](docs/performance/optimization-inventory.md)

Latest verified checkpoint: the compiled desktop navigates in 145.8 ms, idles at 9.40 MB post-GC page heap, and uses 309.45 MB private memory across the full seven-process WebView2 tree; the LovelyGit host accounts for 53.37 MB. Theme catalog bootstrap now retains 27.9 kB instead of 397.3 kB and executes in 0.068 ms instead of 0.79 ms before selected-theme realization. Warm repository-tab content activates in 7.2-13.2 ms using page-side timestamps. Maximum known-repository surfaces now retain 11 tab buttons and 15 recent rows in the real 1,000-record WebView fixture instead of mounting every record; isolated mount latency falls from 412.0 to 43.2 ms for tabs and from 541.8 to 49.0 ms for recent repositories. The same 1,000-record command palette opens in 32.6 ms instead of 72.8 ms and retains 14 result buttons instead of 1,012. Maximum branch comparisons and multi-commit confirmations now retain 14 and seven rows in the real desktop instead of all 100; end-to-end opening takes 83.1 ms and 21.4 ms respectively. Maximum Apply Patch previews now retain nine rows instead of 100 and expose every parsed file, reducing isolated mount latency from 42.9 to 34.5 ms and retained component nodes from 425 to 70. The Worktrees accordion retains 10 virtual rows instead of all 201 in its fixture, reducing the page from 3,405 to 1,124 DOM nodes and from 21.80 MB to 10.16 MB post-GC heap. Unchanged native metadata refresh for 601 refs and 201 worktrees now takes 21.3-21.9 ms per read, including 8.3-8.8 ms for worktree metadata. A 5,001-commit unchanged-path history retains 21.98 MB instead of 28.62 MB while preserving its roughly 470-502 ms traversal time. The 10,102-branch switcher now opens in 92.4 ms with 14 mounted rows, and the merge/rebase branch picker opens in 74.2 ms through CMG (33.2 ms from a direct page-side click) with 14 rows instead of 10,101, reducing its closed-dialog DOM from 21,254 to about 1,148 nodes and page heap from roughly 120-134 MB to 19-20 MB. A 10,601-ref graph hover now retains 26 pills, 1,224 DOM nodes, and 16.39 MB page heap instead of 10,601 pills, 75,224 nodes, and 593.15 MB; commit details derives the same live refs without a backend scan and mounts only three collapsed or 14 expanded pills. A 2,000-file commit details panel now retains 14 changed-file rows instead of 16, keeps all 2,000 reachable, and ordinary commits avoid virtualizer allocation entirely. A 2,000-file stash inspection now retains 17 instead of 22 file rows, while ordinary stashes avoid virtualizer allocation and tracked/untracked models use one exact-size array. Its 100,000-line modified-file view switches Combined, Side-by-Side, Full-file, Changes, and wrapping in 1.1-6.1 ms; bounded compressed-source reuse cuts the whitespace-policy reload from 626.3 to 233.7 ms without retaining the 10 MB source strings, while observed active page heap remains 34.94 MB. Compact ref transport reduces the native envelope from 1,105,168 rejected characters to 66,043 characters. A 5,000-file checkout paints loading feedback in 17.3-42.2 ms, while its variable filesystem completion remains inside Git. A fresh 20-repository desktop audit settles at 13.79 MB observed page heap, 363.22 MB full-tree private memory, and 3,889 handles after 60 tab activations, essentially matching its 364.17 MB / 3,888-handle baseline instead of retaining per repository. The complete backend gate remains below one minute without weakened budgets. The 100,000-line diff and 20,000-file observed-path improvements remain recorded in the linked ledgers.

Fetch, Pull, and Push now await their native Git process instead of reporting success after dispatch. With a deterministic 3.1-second local transport delay, their controls painted disabled in 1-5 ms and remained protected for 3.37-3.50 seconds until Git actually completed.

Successful remote operations now invalidate the active graph directly instead of waiting for the repository watcher. Pull-to-local-ref presentation fell from 186 ms after completion to 5 ms, and fetched remote refs appeared 12 ms after completion.

Frontend wall-clock performance tests now run as a sequential Vitest project after the ordinary parallel suite, preventing unrelated workers from invalidating strict latency budgets. Two complete 709-test gates passed in 50.69 and 49.76 seconds without relaxing any threshold.

Successful branch pushes now request the same authoritative repository refresh as other ref mutations. In the delayed local-transport fixture, the pushed remote branch appeared 62 ms after completion instead of 278 ms while busy feedback remained visible in 18-21 ms.

Maximum-ref local mutations remain inexpensive: cold Create Tag opens in 12.9 ms, paints busy in 13.1 ms, completes Git in 55.5 ms, and presents the ref 60.7 ms later. Branch Rename opens in 22.4 ms, paints busy in 12.2 ms, completes in 88.8 ms, and presents the renamed ref 64.9 ms later. The audit also corrected native WebView input handling that had left Rename disabled despite visible text.

A fresh two-repository, 50,000-commit audit opens the first native graph rows in 249-290 ms. Paging through roughly 6,000 commits retains 1,710 DOM nodes and 14.90 MB post-GC page heap; returning to a cached tab takes 96.8 ms and resets its viewport to the top. Closing the final repository tab now disposes the active native graph immediately through the existing repository-setting lifecycle instead of retaining its pack reader/traversal on New Tab. The visible repository intentionally remains warm between scrolls. Commit search responds in 217.4 ms for a recent common query, including its 140 ms debounce. A complete 50,000-commit native scan now takes 0.87-0.97 s in-process and 1.45 s through the real WebView instead of 4.47-5.03 s, while allocation falls from 305-309 MB to 131-134 MB and only 13 result rows remain mounted. Closing the search cancels active work and releases partial native traversal state immediately. The complete 951-test backend gate remains below one minute.

Large branch integrations keep the desktop responsive while Git updates the worktree. In a disposable 1,000-file plus 100,000-line fixture, Merge and Rebase painted their disabled busy states in 19.4-26.9 ms and 22.8 ms respectively, remained protected until completion, and produced the exact clean Git state. Native Merge samples reported 169-189 MB host working set and 51-67 MB private memory. Git Trace2 attributes the remaining 4.4-6.5 s completion time to Git's worktree checkout inside the desktop process rather than LovelyGit lookup, transport, or graph reconciliation; forcing one checkout worker regressed Merge to 7.36 s and was rejected.

Large commit mutations use the same immediate protection. Cherry-pick, Revert, and destructive Hard Reset painted disabled feedback in 19.2 ms, 20.9 ms, and 16.2 ms while preserving confirmation and exact clean Git outcomes. Their 4.54 s, 1.18 s, and 1.09-4.30 s native completion times remain inside Git; direct four-worker Git completed the same fixtures in 0.99 s, 0.76 s, and 0.67 s. The host stayed below 205 MB working set and 67 MB private memory across the observed commands.

Apply Patch now relies on Git's atomic apply transaction instead of launching a separate `git apply --check` process before the identical real apply. A 3.97 MB patch spanning 1,001 files falls from 2,169.4 ms to 2,025.2 ms on average, saving 144.3 ms and one process launch. Multi-file regressions prove a later invalid hunk leaves an earlier valid file, the worktree, and the index unchanged in both staged and unstaged modes; reverse and pre-cancelled paths remain covered.

A fresh retained-memory audit after warming four disposable repositories leaves only 8.93 MB in the managed heap and 8.48 MB in the page heap after forced collection, with 799 DOM nodes. The LovelyGit host uses 45.6-48.1 MB private and the packaged-app-equivalent host/WebView2 subtree uses 297.4 MB private; the separate `dotnet run` launcher is development overhead and is not counted. No large first-party retained graph remains, so the bounded interaction caches were preserved.

Maximum sparse-checkout mutation preparation now scans cone paths directly and sizes its bounded deduplication table once. Normalizing 100,000 paths falls from 40.02 ms / 34.17 MiB allocated to 22.02 ms / 18.36 MiB, reducing pre-Git latency 44.98% and allocation 46.27% while retaining exact validation.

A real 5,000-file submodule Initialize paints disabled feedback in 1.1 ms and reaches authoritative `Current` state in 3.64 seconds, including Git and the native refresh. Direct Git took 6.20 seconds in the same disposable fixture, confirming the existing asynchronous LovelyGit path is not the bottleneck and should not gain an artificial paint delay.

## Measurement Rules

- Measure from a healthy runner state and use disposable repositories only.
- Use Chromium-sized repositories for large-repository checks and CMG for WebView interaction timing.
- Report cold and warm behavior when caches materially affect the result.
- Treat latency, allocations, retained memory, payload size, process count, and test duration as separate budgets.
- Do not claim a win without a before/after measurement or a focused regression benchmark.

## Mutation Workflow Baselines

Measured through the same Git commands LovelyGit uses, primarily in a disposable 2,001-file repository with an 8 MiB file; rows identify the larger 20,000-file fixture where used. These are investigation baselines, not all optimization claims.

| Workflow | Observed latency |
| --- | ---: |
| Create/delete branch or tag | 44-50 ms |
| Checkout roughly 1,000 changed paths | 0.97-1.03 s after bounded parallelization |
| Stash 501 changed files | 3.83 s cold; 1.33-1.39 s warm |
| Restore 501-file stash | 1.06 s |
| Create/remove 2,001-file worktree | 1.28-1.36 s / 586 ms after bounded parallelization |
| Create/remove 20,000-file worktree | 5.45 s / 6.65 s direct Git; removal busy state paints in 3.8-4.4 ms |
| Cherry-pick/revert 100 changed files | 251 ms / 183 ms |
| Merge 500 changed files | 1.02 s |
| Rebase 100 changed files | 894 ms |
| Merge / rebase 1,000 files plus one 100,000-line file | Busy paint 19.4-26.9 ms / 22.8 ms; desktop completion 4.40-4.59 s / 6.52 s; exact direct Git 0.95-1.12 s / 1.72 s |
| Cherry-pick / revert / hard reset 1,000 files plus one 100,000-line file | Busy paint 19.2 ms / 20.9 ms / 16.2 ms; native completion 4.54 s / 1.18 s / 1.09-4.30 s; exact direct Git 0.99 s / 0.76 s / 0.67 s |
| Apply 3.97 MB / 1,001-file patch | 2,169.4 ms with redundant preflight to 2,025.2 ms atomic apply; 144.3 ms / 6.7% faster and one fewer Git process |
| Stage 1,000 of 20,000 tracked files | 7.18 s cold CMG completion; 0.70-0.90 s warm service runs |
| Unstage 1,000 of 20,000 tracked files | 749 ms CMG completion; 0.31-0.47 s warm service runs |
| Discard 1,000 of 20,000 tracked files | 87 ms warm CMG completion; 0.65-0.67 s cold service runs |
| Commit 1,000 staged files in a 20,000-file repository | 0.23-0.34 s service; 439.2 ms CMG control settlement after deferring auto-maintenance |
| Fetch / Pull / Push completion contract | Disabled feedback in 1-5 ms; awaited 3.37-3.50 s delayed transport process |
| Remote completion to authoritative refs | Pull 186 ms to 5 ms; Fetch 12 ms measured after the change |
| Branch push completion to remote ref | 278 ms to 62 ms; busy feedback remains visible in 18-21 ms |
| 500-tag create / 102-branch rename | Tag: 12.9 ms dialog, 13.1 ms busy, 55.5 ms Git, 60.7 ms reconciliation; rename: 22.4 ms dialog, 12.2 ms busy, 88.8 ms Git, 64.9 ms reconciliation |
| 50,000-commit graph retention | Cold rows 249-290 ms; roughly 6,000 paged commits retain 14.90 MB post-GC page heap and 1,710 DOM nodes; warm tab return 96.8 ms at scroll top; close-last-tab releases the native graph |
| 50,000-commit search | Recent common query 217.4 ms including 140 ms debounce; complete native traversal 4.47-4.89 s to 0.87-0.97 s and 305-309 MB to 131-134 MB allocated; real WebView 4.92-5.03 s to 1.45 s; 13 mounted rows |

## Rejected or Deferred Experiments

| Experiment | Observed benefit | Reason not shipped |
| --- | --- | --- |
| Enable Git FSMonitor for checkout | Chromium checkout improved from about 2.1 s to 0.85–0.90 s warm | Persistent daemon retained about 37.8 MB per repository and the first full status scan regressed to about 3.5 s. |
| Enable Git untracked cache automatically | Warm status reached about 0.41–0.44 s | Initial warm-up was about 3.6 s and mutating user repository configuration was not justified. |
| Suppress working-tree watchers during checkout | No repeatable improvement | Added lifecycle complexity without measurable value; reverted. |
| Swap Git executable path for checkout | No repeatable improvement | Nested app-process launch cost remained; reverted. |
| Replace standard stash push with `--quiet` or create/store/reset | None; warmed standard was 1.33 s, quiet 1.37 s, custom 1.47 s | Standard Git semantics were both fastest and safest, so the alternatives were rejected. |
| Bypass object caches while peeling loose tags | Cold Chromium-scale graph open regressed from 281.86 ms / 5.21 MB allocated to 319.37 ms / 7.55 MB | Many lightweight tags share commit targets, so the existing bounded object cache avoids repeated pack reads; retained unchanged resolver. |
| Batch submodule HEAD tree lookups with a path trie | Allocations fell from 2.53 MB to 2.18 MB for 1,000 definitions, but latency moved from 59.29 ms to 60.18 ms | Path normalization and worktree state checks dominate the manager read; the extra parser complexity did not improve interaction latency, so it was removed. |
| Replace async worktree fan-out with synchronous `Parallel.For` | Allocations remained about 43.5 MB and ten-read latency regressed from 304.30 ms to 325.15 ms in the intermediate small-file experiment | The existing bounded async scheduler was retained; allocation was subsequently addressed at the small-file buffer layer instead. |
| Cache worktree snapshots behind per-file metadata fingerprints | Allocations fell from about 3.77 MB to 2.39 MB, but ten unchanged reads regressed from roughly 274 ms to 333 ms | Validating hundreds of files costs nearly as much as reading them on Windows and introduces cache complexity; direct bounded reads remain faster and immediately fresh. |
| Keep only the top 100 painted branch-comparison commits during traversal | Repeated 10,000-commit runs remained at 65.85-65.99 MB and latency varied within the prior range | Sorting the displayed subset is not the bottleneck; packed commit decoding dominates, so the more complex bounded selector was removed. |
| Force the native status scanner across a 20,000-entry index | 822 ms and 10.5 MB allocated versus Git's roughly 48-53 ms warm scan | The existing 1,000-entry crossover policy remains correct; wide repositories should use Git's optimized index/stat traversal. |
| Replace streamed single-file pathspec input with a direct `git add -- <path>` argument | Both forms remained roughly 52-57 ms on the 20,000-file fixture | Git index rewrite cost dominates; retained the shared streamed pathspec path for consistent validation and batching. |
| Replace bulk `git add -A` with `git add -u` for tracked-only changes | `add -u` took about 579 ms versus 537 ms for `add -A` on the 20,000-file fixture | It was slower and would exclude untracked files from Stage All, so the existing command was retained. |
| Detach post-commit status reconciliation | No improvement; the graph updated in 441 ms but the commit control remained busy for about 1.38 s | Trace2 proved the delay was inside Git's automatic maintenance, not LovelyGit's reconciliation, so the experiment was reverted. |
| Add only `--quiet` to foreground commit | Highly variable at 192-1,095 ms versus 361-1,906 ms without it | Output was not the bottleneck; automatic maintenance dominated. LovelyGit instead disables auto-maintenance for that invocation and schedules it after success. |
| Hard-link repository-template files in the backend suite | Full-suite time regressed from 98.4 s to 100.3 s and retained the same two contention-sensitive failures | NTFS link creation and the remaining fixture setup dominated; reverted to isolated physical copies to preserve straightforward ownership. |
| Let the serialized performance collection overlap ordinary tests | Full-suite time improved from 94.7 s to 86.2 s | Still exceeded the one-minute requirement and retained timing-budget failures under contention, so global isolation was restored. |
| Increase xUnit's maximum parallel threads to eight | The complete gate exceeded 120 s | Extra process and filesystem contention made the suite slower, so the default scheduler was restored. |
| Share template Git objects through alternates | No repeatable wall-clock improvement | It changed which object store the commit-graph reader observed and complicated fixture ownership, so exclusive physical copies were retained. |
| Split performance tests into six in-process lanes | Testhost wall time reached 59.83 s | `GC.GetTotalAllocatedBytes` is process-global; concurrent measurements caused 14 false allocation/latency failures, so the isolated collection was restored. |
| Run six performance lanes in separate testhosts | The isolated lane phase completed in 24.5 s | Simultaneous Git/filesystem workloads caused three valid latency gates to fail under contention, so the safe single-testhost gate remains authoritative. |
| Increase parallel-checkout settings for stash | Cold 501-of-20,000-file stash moved only from 4.19 s to 4.10 s | LovelyGit already supplies four checkout workers; Trace2 showed the temporary-index update dominates, so no additional Git configuration was added. |
| Prefer Git for Windows' `cmd\\git.exe` launcher over `mingw64\\bin\\git.exe` | No improvement: the 5,000-file in-app checkout completed in 23.13 s through the launcher versus 21.46-23.02 s through the direct executable | Executable selection is not the bottleneck, so the established direct executable and helper-path discovery remain unchanged. |
| Raise global checkout workers from four to eight or sixteen | Direct 5,000-file switches improved from 2.81/3.85 s with four workers to 2.42/3.01 s with eight and 2.26/2.66 s with sixteen | The extra four to twelve transient Git worker processes trade substantial peak memory for a modest gain and did not address the in-app pre-worker filesystem phase; the bounded four-worker policy remains. |
| Disable parallel checkout for large integration operations | The experiment removed transient checkout workers | The same 1,000-file Merge regressed from 4.40-4.59 s with four bounded workers to 7.36 s with one worker. Trace2 showed the single-worker checkout itself consumed the extra time, so the established four-worker policy remains. |
| Disable WebView2 GPU acceleration | Compiled private memory fell from 302.92 MB to 212.95 MB; 500-tag and 100,000-line-diff virtual scrolling retained 120 Hz / zero frames over 20 ms | Warm two-frame tab activation regressed from 16.72 ms to 18.61 ms average, large-diff scrolling was unchanged, and [Microsoft's WebView2 performance guidance](https://learn.microsoft.com/microsoft-edge/webview2/concepts/performance) says GPU rendering is critical and should only be disabled for troubleshooting. Hardware acceleration remains enabled. |
| Replace or virtualize the appearance theme selector | The real compiled WebView opens 49 light-theme options in 27.3 ms, adds only 210 DOM nodes, and remains at about 8.5 MB observed page heap | This is already comfortably inside the interaction budget and uses the design system's custom scrollbar; a heavier searchable picker would add complexity and bundle weight without a measured user-visible win. |
| Replace native deep commit search with `git log --grep` | Direct Git scans the packed 50,000-commit fixture in roughly 291-341 ms; the optimized native core is now 0.87-0.97 s instead of 4.47-4.89 s, with 1.45 s measured through the real WebView | LovelyGit's read architecture deliberately keeps commit discovery in-process, and the CLI result would discard the resumable session and native filter pipeline. The remaining gap stays visible for later profiling, but no longer justifies silently changing engines. |

## Next Measurement Areas

- Large network clone/fetch/pull/push transfer memory, credential hand-off, and cancellation against a controlled remote where network variance can be isolated.
- Destructive and failure-path timing for submodule, LFS, bisect, and interactive-rebase execution; their list rendering and cold dialogs are already measured.
- Pack/cache invalidation under repeated remote repacks and long-session resource scaling beyond the measured 20-tab, overlay, handle, transaction, and four-repository heap baselines.
- Packaged cold/warm startup across slower storage tiers and repository activation after operating-system file-cache eviction.
