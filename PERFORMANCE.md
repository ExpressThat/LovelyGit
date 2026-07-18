# LovelyGit Performance Ledger

This is the durable index of LovelyGit performance work. Update the linked ledgers in the same commit as every performance change. Record the user-visible path, the optimization, evidence, memory effect, and commit. Keep rejected experiments so future work does not repeat them.

## Detailed Ledgers

- [Verified performance results](docs/performance/verified-results.md)
- [Completed optimization inventory](docs/performance/optimization-inventory.md)

Latest verified checkpoint: the compiled desktop navigates in 145.8 ms, idles at 9.40 MB post-GC page heap, and uses 309.45 MB private memory across the full seven-process WebView2 tree; the LovelyGit host accounts for 53.37 MB. Warm repository-tab content activates in 7.2-13.2 ms using page-side timestamps. Maximum known-repository surfaces now retain 11 tab buttons and 15 recent rows in the real 1,000-record WebView fixture instead of mounting every record; isolated mount latency falls from 412.0 to 43.2 ms for tabs and from 541.8 to 49.0 ms for recent repositories. The same 1,000-record command palette opens in 32.6 ms instead of 72.8 ms and retains 14 result buttons instead of 1,012. Maximum branch comparisons and multi-commit confirmations now retain 14 and seven rows in the real desktop instead of all 100; end-to-end opening takes 83.1 ms and 21.4 ms respectively. Maximum Apply Patch previews now retain nine rows instead of 100 and expose every parsed file, reducing isolated mount latency from 42.9 to 34.5 ms and retained component nodes from 425 to 70. The Worktrees accordion retains 10 virtual rows instead of all 201 in its fixture, reducing the page from 3,405 to 1,124 DOM nodes and from 21.80 MB to 10.16 MB post-GC heap. The 10,102-branch switcher now opens in 92.4 ms with 14 mounted rows, and the merge/rebase branch picker opens in 74.2 ms through CMG (33.2 ms from a direct page-side click) with 14 rows instead of 10,101, reducing its closed-dialog DOM from 21,254 to about 1,148 nodes and page heap from roughly 120-134 MB to 19-20 MB. A 10,601-ref graph hover now retains 26 pills, 1,224 DOM nodes, and 16.39 MB page heap instead of 10,601 pills, 75,224 nodes, and 593.15 MB; commit details derives the same live refs without a backend scan and mounts only three collapsed or 14 expanded pills. Compact ref transport reduces the native envelope from 1,105,168 rejected characters to 66,043 characters. A 5,000-file checkout paints loading feedback in 17.3-42.2 ms, while its variable filesystem completion remains inside Git. The 100,000-line diff and 20,000-file observed-path improvements remain recorded in the linked ledgers, and the backend gate's strict sub-minute target remains open.

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
| Stage 1,000 of 20,000 tracked files | 7.18 s cold CMG completion; 0.70-0.90 s warm service runs |
| Unstage 1,000 of 20,000 tracked files | 749 ms CMG completion; 0.31-0.47 s warm service runs |
| Discard 1,000 of 20,000 tracked files | 87 ms warm CMG completion; 0.65-0.67 s cold service runs |
| Commit 1,000 staged files in a 20,000-file repository | 0.23-0.34 s service; 439.2 ms CMG control settlement after deferring auto-maintenance |

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
| Disable WebView2 GPU acceleration | Compiled private memory fell from 302.92 MB to 212.95 MB; 500-tag and 100,000-line-diff virtual scrolling retained 120 Hz / zero frames over 20 ms | Warm two-frame tab activation regressed from 16.72 ms to 18.61 ms average, large-diff scrolling was unchanged, and [Microsoft's WebView2 performance guidance](https://learn.microsoft.com/microsoft-edge/webview2/concepts/performance) says GPU rendering is critical and should only be disabled for troubleshooting. Hardware acceleration remains enabled. |
| Replace or virtualize the appearance theme selector | The real compiled WebView opens 49 light-theme options in 27.3 ms, adds only 210 DOM nodes, and remains at about 8.5 MB observed page heap | This is already comfortably inside the interaction budget and uses the design system's custom scrollbar; a heavier searchable picker would add complexity and bundle weight without a measured user-visible win. |

## Next Measurement Areas

- Branch, tag, remote, stash, worktree, submodule, LFS, sparse checkout, bisect, rebase, merge, cherry-pick, revert, reset, and patch workflows.
- App startup, repository activation, tab switching, settings, command palette, dialogs, and every lazy overlay.
- Clone progress, authentication hand-off, cancellation, and large-transfer memory; repeat fetch/pull/push against a representative network remote where network variance can be isolated.
- Cold/warm cache invalidation cost and long-session resource scaling beyond the measured overlay, tab, handle, and transaction baselines.
