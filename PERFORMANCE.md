# LovelyGit Performance Ledger

This is the durable index of LovelyGit performance work. Update the linked ledgers in the same commit as every performance change. Record the user-visible path, the optimization, evidence, memory effect, and commit. Keep rejected experiments so future work does not repeat them.

## Detailed Ledgers

- [Verified performance results](docs/performance/verified-results.md)
- [Completed optimization inventory](docs/performance/optimization-inventory.md)

Latest verified checkpoint: Chromium-scale cold graph activation now bounds loose-tag enumeration and avoids random page-cache churn on very large pack indexes; see the linked ledgers for measurements and coverage.

## Measurement Rules

- Measure from a healthy runner state and use disposable repositories only.
- Use Chromium-sized repositories for large-repository checks and CMG for WebView interaction timing.
- Report cold and warm behavior when caches materially affect the result.
- Treat latency, allocations, retained memory, payload size, process count, and test duration as separate budgets.
- Do not claim a win without a before/after measurement or a focused regression benchmark.

## Mutation Workflow Baselines

Measured through the same Git commands LovelyGit uses, in a disposable 2,001-file repository with an 8 MiB file. These are investigation baselines, not all optimization claims.

| Workflow | Observed latency |
| --- | ---: |
| Create/delete branch or tag | 44-50 ms |
| Checkout roughly 1,000 changed paths | 0.97-1.03 s after bounded parallelization |
| Stash 501 changed files | 3.83 s cold; 1.33-1.39 s warm |
| Restore 501-file stash | 1.06 s |
| Create/remove 2,001-file worktree | 1.28-1.36 s / 586 ms after bounded parallelization |
| Cherry-pick/revert 100 changed files | 251 ms / 183 ms |
| Merge 500 changed files | 1.02 s |
| Rebase 100 changed files | 894 ms |

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

## Next Measurement Areas

- Branch, tag, remote, stash, worktree, submodule, LFS, sparse checkout, bisect, rebase, merge, cherry-pick, revert, reset, and patch workflows.
- App startup, repository activation, tab switching, settings, command palette, dialogs, and every lazy overlay.
- Clone progress, authentication hand-off, cancellation, and large-transfer memory; repeat fetch/pull/push against a representative network remote where network variance can be isolated.
- Cold/warm cache invalidation cost and long-session resource scaling beyond the measured overlay, tab, handle, and transaction baselines.
