# LovelyGit Performance Ledger

This is the durable inventory of LovelyGit performance work. Update it in the same commit as every performance change. Record the user-visible path, the optimization, evidence, memory effect, and commit. Keep rejected experiments so future work does not repeat them.

## Measurement Rules

- Measure from a healthy runner state and use disposable repositories only.
- Use Chromium-sized repositories for large-repository checks and CMG for WebView interaction timing.
- Report cold and warm behavior when caches materially affect the result.
- Treat latency, allocations, retained memory, payload size, process count, and test duration as separate budgets.
- Do not claim a win without a before/after measurement or a focused regression benchmark.

## Verified Results

| Area | Before | After | Fixture and evidence | Commit |
| --- | ---: | ---: | --- | --- |
| Commit graph random scroll | 24.5 ms average; 2,305 DOM nodes | 13.9 ms average; 1,676 DOM nodes | Chromium CMG scroll journey; virtual overscan 36 to 8 | `7180d47` |
| Large worktree status | 596 ms | 75.7 ms | 10,000-file disposable repository; choose Git status above the native scan threshold | `0a8d9e1` |
| Large checkout / worktree creation | 1.69-1.82 s / 2.21-2.28 s | 0.97-1.03 s / 1.28-1.36 s | Roughly 1,000 changed paths / 2,001-file worktree; bounded four-worker checkout above 100 paths | `db9ea3b` |
| No-match commit search | 1,698 ms | 536 ms | Chromium CMG search journey; bounded first result budget | `dc5b68c` |
| Chromium working changes | 1.49 s with no useful intermediate result | 1.07 s tracked result; 1.62 s complete result | CMG refresh; concurrent tracked-only and complete scans | `3a4bcbd` |
| Cold Settings / Command Palette | 321.5 ms / 315.4 ms | 46.0 ms / 19.7 ms | Chromium CMG click-to-dialog timing; immediate deferred reveal while preserving lazy chunks | `f017242` |
| Cold Commit Search | 319.6 ms | 23.4 ms | Chromium CMG click-to-dialog timing; immediate deferred reveal while preserving lazy loading | `5378470` |
| Cold Remote Manager | 307.6 ms | 16.5 ms | Chromium CMG click-to-dialog timing; immediate deferred reveal while preserving its 13.15 kB lazy chunk | `a4ac6fe` |
| Cold Rename Branch | 314.4 ms | 24.8 ms | Chromium CMG context-menu-to-dialog timing; immediate deferred reveal while preserving its lazy chunk | `7cff88b` |
| Cold Cherry-pick | 312.8 ms | 21.6 ms | Chromium CMG commit-context-menu-to-dialog timing; immediate deferred reveal while preserving its lazy chunk | `5729cad` |
| Cold Delete Tag | 314.7 ms | 22.4 ms | Chromium CMG tag-context-menu-to-confirmation timing; immediate deferred reveal while preserving its lazy chunk | `cfc2122` |
| Cold Create Worktree | 316.2 ms | 20.6 ms | Chromium CMG branch-context-menu-to-dialog timing; immediate deferred reveal while preserving its lazy chunk | `fd2142a` |
| Cold Reflog | 312.4 ms | 21.2 ms | Chromium CMG branch-context-menu-to-dialog timing; immediate deferred reveal while preserving its 9.27 kB lazy chunk | `a0dcf12` |
| Cold Git LFS Manager | 304.6 ms | 14.5 ms | Chromium CMG toolbar-click-to-dialog timing; shared immediate deferred reveal preserving independent manager chunks | `b1fc2e6` |
| Cold Commit Identity | 310.1 ms | 34.5 ms | Chromium CMG working-changes-click-to-dialog timing; immediate deferred reveal preserving its lazy dialog chunk | `74f068a` |
| Cold Remote Branch Checkout | 311.4 ms | 17.5 ms | Disposable-remote CMG context-menu-to-dialog timing; immediate deferred reveal preserving the combined remote-dialog chunk | `a2648d6` |
| Warm Working Changes Content | 310.5 ms | 44.2 ms | Chromium CMG toolbar-click-to-meaningful-content timing; immediate deferred reveal preserving the 43.45 kB lazy chunk | `3cb31fa` |
| Cold New Tab | 309.0 ms | 27.2 ms | Chromium CMG plus-click-to-onboarding timing; immediate deferred reveal preserving the separate New Tab chunk | `88b7b26` |
| Cold Stash File Diff | 314.1 ms | 15.6 ms | Disposable-stash CMG file-click-to-diff timing; immediate deferred reveal preserving the 6.68 kB commit-diff chunk | `95b00d5` |
| Cold Bisect Session Content | 304.0 ms | 13.5 ms | Chromium CMG toolbar-click-to-meaningful-content timing; immediate deferred reveal preserving the 5.77 kB session chunk | `79a6057` |
| Cold Force Push Confirmation | 318.2 ms | 13.4 ms | Chromium CMG menu-item-to-safety-dialog timing; immediate deferred reveal preserving the 2.14 kB confirmation chunk | `1f749da` |
| Repeated overlay retention | 10.88 MB post-GC heap | 13.61 MB after 60 opens; 13.83 MB after 120 | Chromium CMG Settings/Bisect/Force Push cycles; second pass +0.22 MB and zero live-DOM growth | `9913036` |
| Working Changes retention | 13.97 MB warmed post-GC heap | 15.31 MB after 20+ opens; 15.39 MB after 10 more | Chromium CMG panel cycles; second pass +0.08 MB and live DOM stable at 1,687 nodes | `9913036` |
| Bisect handle retention | 1,065 host handles | 1,059 after 30 additional reads and 2 s settle | Chromium CMG read-only session loads; no linear native handle growth | `9913036` |
| Warm four-tab desktop footprint | Not previously recorded | 517.9 MB private; 825.3 MB working set; 4,420 handles | LovelyGit host plus owned WebView2 tree after broad Chromium audit; host itself was 121.1 MB private | `9913036` |
| Repeated fetch/repack pack handles | One pack and index handle retained per obsolete pack generation until repository disposal | Only the current generation remains; in-flight old readers release their exact handles on completion | Disposable packed repository repacked with `gc --prune=now`; native parser resource regression | `986719d` |
| Working-tree watcher path retention | Every uniquely observed path remained in the duplicate-suppression dictionary until repository switch | Entries expire after the 500 ms suppression horizon; one reusable timer and no per-event cleanup queue | Real `FileSystemWatcher` regression verifies publication, temporary retention, and automatic release | `fc63548` |
| Commit-search retention timer | Periodic 30-second wake-up and collection snapshot for the entire app lifetime, including while idle | One-shot timer exists only while a partial native search session is retained; idle searches perform no timer work | Session lifecycle regression verifies idle, retained, consumed, and expired scheduling states | `f1f97be` |
| Resize-drag listener ownership | Four resize surfaces depended on a future pointer-up to release global listeners | Shared lifecycle-owned drag hook releases on pointer-up, pointer cancellation, window blur, replacement, and component unmount | Frontend regression exercises completion, cancellation, and mid-drag unmount | `029e30b` |
| Native bridge send failure | A synchronous host-send failure retained its pending request and timeout for 10 seconds | Send failure immediately clears both before rejecting; late responses cannot record false timing | Transport regression asserts zero timers and no late-response performance event after failure | `fff3f59` |
| Loose repository-ref refresh | 188.61 ms and 21.60 MB allocated for five reads of 500 loose refs | 160.15 ms and 1.84 MB allocated; normal ref files parse from a stack buffer and tag counting stays single-pass | Isolated disposable 500-ref benchmark; 14.95% faster and 91.48% fewer allocated bytes | `dcb5d89` |
| Native repository loose-ref load | 625.47 ms and 22.44 MB allocated for five cold-cache loads of 500 loose refs | 615.16 ms and 2.68 MB allocated; shares the stack-buffer reader with repository-ref refresh | Isolated disposable multi-directory benchmark; latency effectively unchanged and allocated bytes reduced 88.06% | `c609410` |
| Optimistic bulk unstage | 118.97 ms for 10,000 partially staged files due to repeated linear working-file lookup | 3.49 ms using one path index for the bulk mutation | Pure frontend large-list benchmark; 97.07% lower synchronous interaction latency with a 50 ms regression budget | `1a8771d` |
| Observed working-tree burst | 14.49 ms to apply 25 observed paths to a 20,000-file state with one full-list filter per path | 6.21 ms using a single partition pass while preserving multi-group path state | Pure frontend maximum optimistic-burst benchmark; 57.14% lower latency with a 12 ms regression budget | `8443d36` |
| Sparse graph refs-panel preparation | 15.10 ms for 500 refs across a 500,000-slot sparse graph, including per-slot temporary arrays and a second full scan | 11.06 ms with a single allocation-light scan, bounded row lookup, and authoritative repository refs | Pure frontend large-graph benchmark; 26.75% lower latency with an 18 ms noise-tolerant regression budget | `af33902` |
| Dense commit ref grouping | Not previously recorded | 42.04 ms for 100 preparations of 500 mixed refs (0.42 ms each) | Pure frontend grouping baseline with local/remote pairing and current-branch ordering | `af33902` |
| Native blame of alternating large edit | 178.00 ms and 9.18 MB allocated for a 20,000-line alternating edit | 81.71 ms and 9.07 MB allocated with linear in-place active-line partitioning | Disposable real-Git two-commit fixture; 54.10% lower latency with exact alternating attribution checks | `47e8ba4` |
| Remote sync without an upstream | 87.36 ms and 5.10 MB allocated for 100 refreshes | 68.26 ms and 3.23 MB allocated by sharing one parsed `HEAD` snapshot | Disposable real-Git repository; 21.86% lower latency and 36.52% fewer allocated bytes with a permanent regression budget | `c8dc3cd` |
| Divergent branch comparison | 630.99 ms and 53.71 MB allocated for 10,000 divergent commits; traversal objects entered the process-wide cache | 613.03 ms and 51.34 MB allocated; traversal cache is repository-scoped and released after comparison | Deterministic packed real-Git fixture; exact 5,000-ahead/5,000-behind checks plus a shared-cache retention assertion | `8dceece` |
| Real remote clone | Not previously recorded | 4.91 s; 42.5 MiB checkout; 34.99 MiB pack; +21.7 MB observed desktop private memory | Full `sharkdp/bat` clone through CMG; 20,693 objects with monotonic overall and phase progress | `d795e85` |
| Complete backend test gate | Previously over one minute during early integration work | 55.89 s clean run; established baseline 30–36 s | `Invoke-LovelyGitTestGate.ps1`, 574 tests at this checkpoint | `021c0ee`, `089f559`, `3a4bcbd` |

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

## Completed Optimization Inventory

### Startup, Bundles, and Desktop Runtime

- Moved update downloads off startup and removed unused startup credential access (`ead5b51`, `c28cd5b`).
- Lazy-loaded settings, repository onboarding, graph operations, management dialogs, and toolbar workflows (`b0ddbf0`, `5ab026e`, `e63b974`, `350b9af`, `bd256d1`, `509e26c`).
- Deferred Motion and the icon sprite, released closed overlays, and balanced overlay retention (`7068517`, `02b3c0e`, `2f58f61`).
- Removed React Suspense's cold reveal delay from Settings, Command Palette, Commit Search, stash, history, and blame without eagerly loading those tools (`f017242`, `5378470`).
- Removed the same cold reveal delay from create-branch, merge/rebase, and remote-management dialogs while preserving their separate lazy chunks (`a4ac6fe`).
- Applied immediate deferred reveal to branch comparison, upstream, rename, and delete dialogs (`7cff88b`).
- Applied immediate deferred reveal to cherry-pick, revert, reset, detached checkout, and interactive-rebase dialogs (`5729cad`).
- Applied immediate deferred reveal to create-tag, checkout-tag, and local-tag deletion dialogs (`cfc2122`).
- Applied immediate deferred reveal to create, lock, and remove-worktree dialogs while retaining their separate lazy chunks (`fd2142a`).
- Applied immediate deferred reveal to reflog browsing and its nested reset confirmation while preserving separate lazy chunks (`a0dcf12`).
- Applied immediate deferred reveal to Git LFS, sparse-checkout, and submodule managers while preserving their independent chunks (`b1fc2e6`).
- Applied immediate deferred reveal to commit-identity editing while preserving its separate dialog chunk (`74f068a`).
- Applied immediate deferred reveal to remote-branch checkout and deletion confirmations while preserving their shared chunk (`a2648d6`).
- Applied immediate deferred reveal to repository onboarding/New Tab while preserving its separate lazy chunk (`88b7b26`).
- Applied immediate deferred reveal to meaningful bisect-session content while preserving its separate session chunk (`79a6057`).
- Applied immediate deferred reveal to force-with-lease confirmation while preserving its separate safety-dialog chunk (`1f749da`).
- Switched the desktop process to workstation GC and moved process-memory sampling off interaction paths (`b0d124b`, `c7241dc`).
- Reduced native interaction metrics overhead (`efcd0a7`).

### Commit Graph, Refs, and Tabs

- Compacted graph payloads by removing unused parents, flags, signatures, duplicate ref indexes, and page copies (`6379222`, `398f54f`, `6a88ce5`, `944408d`, `233b298`, `d0973d3`, `4a5a87f`).
- Reduced graph render and lane allocations and skipped unnecessary ref grouping (`6adc6b9`, `528d103`, `0f4b7d9`, `adac8bc`, `5367bef`).
- Kept active traversal warm, cached bounded tab previews, preserved caches across reloads, and disposed idle sessions (`2ee809d`, `4e6d8d3`, `cd6124a`, `6275027`).
- Reduced initial paging/retention, cancelled abandoned refreshes, and coalesced warm activation (`9a1856b`, `30547fd`, `4d29e3d`, `aec7cd9`, `e207118`).
- Cached bounded ref snapshots, streamed peeled refs, reused refs across controls, and reduced loose-ref loading (`24806de`, `5ad6053`, `4e2ad7c`, `ede92d7`, `4c47c80`).
- Reset scroll on tab switches and reduced virtualized graph overscan (`6b0fcf6`, `ca11dfd`, `7180d47`).

### Native Git Parser and Object Storage

- Accelerated packed commit reads, shared a bounded object cache, and supported alternate object stores (`a3ecfc4`, `c8e17b4`, `bedf3b6`).
- Avoided opening object stores without upstreams and avoided search pollution of the object cache (`93dde92`, `f4c3080`).
- Resolved abbreviated hashes from indexes and reduced native graph parsing allocations (`8aad229`, `89d0a34`).
- Eliminated per-index-entry scan allocations and reused compiled ignore matchers (`353786e`, `dbc5386`).
- Added a packed-graph performance regression gate (`81514be`).

### Commit Search and Details

- Returned recent matches quickly, resumed deep searches, scoped search to refs, and added native author/date/text search (`89341fc`, `1af01ab`, `932d835`, `20871c7`, `66917ba`, `dc5b68c`).
- Removed duplicate graph work during search and bounded ref suggestions (`f4c3080`, `c03ac25`).
- Prefetched details on hover intent and rendered prefetched details without suspense (`e346227`, `bda6b62`, `2cd7423`).
- Moved details parsing/persistence off click paths, bulked cache writes, and reduced blob/parser allocations (`c03064b`, `7ce891c`, `ddc34cd`, `021b3dd`, `d461dc6`).

### Working Tree, Staging, and Status

- Removed the Suspense reveal delay from Working Changes and working-tree diffs while preserving their separate lazy chunks; the disposable diff opened in 53.1 ms and its display controls responded in 0.5–2.1 ms (`3cb31fa`).
- Removed the nested Suspense reveal delay from stashed-file inspection while preserving the commit-diff chunk; its diff controls and add/remove colors remain shared with the verified diff surface (`95b00d5`).
- Added immediate optimistic stage/unstage previews and kept them stable during refresh (`7db0b3d`, `15ebece`).
- Accelerated single-file index updates and avoided duplicate staged/index scans (`c1dc63e`, `67f1d6a`, `f662e04`).
- Coalesced notifications and reconciliation scans and reused background scans (`3c47512`, `7ccc7c8`, `69cc4e1`, `e6bf212`).
- Added preliminary summaries, bounded preload, deferred heavy background preloads, and trusted complete cached summaries (`4032430`, `5824b04`, `1d6fbfa`, `f370993`).
- Bounded wide-repository background memory and skipped oversized native scans (`8a2fd98`, `b580871`, `0a8d9e1`).
- Added progressive tracked-first working changes with explicit completeness and disabled mutation controls until complete (`3a4bcbd`).
- Enabled command-local, four-worker parallel checkout above 100 paths for switch, clone, worktree, reset, merge, rebase, cherry-pick, and revert operations. This avoids persistent repository configuration and daemon memory while bounding transient worker cost (`db9ea3b`).

### Diff Engine and File Diffs

- Replaced DiffPlex with the single low-allocation diff engine (`a1625fc`).
- Trimmed unchanged edges, vectorized line preparation, reduced line splitting/context allocations, and accelerated ignored-whitespace diffs (`91de674`, `5ca8e3e`, `9ee5854`, `470bc1c`, `5890323`).
- Virtualized and bounded large/long-line rendering and halved side-by-side row work (`30404a9`, `4dd7b33`, `6c4a42b`, `4429b34`, `df83dfe`).
- Compacted, compressed, referenced, and reused large colored diff payloads across layouts (`b55def8`, `f4f6d53`, `46af1fd`, `80d6769`, `ac7fb8a`, `df0c41b`, `d10ac7b`).
- Bounded decoded/source/result cache weight and skipped uncacheable prewarming (`15ce59d`, `9f613ed`, `c4e610c`, `70eecfd`).
- Moved diff persistence off click paths and cached open variants (`b074af7`, `dc120ec`, `54cb0c7`, `3726952`).

### Conflict Resolution

- Virtualized large outputs/gutters and accelerated display changes (`c921509`, `0a25e71`, `d955b50`, `13635ca`).
- Reused worktree snapshots, source lines, prepared models, view variants, and unchanged text (`a204ebf`, `cf67ef4`, `0cdb614`, `1880b11`, `11e8732`).
- Reduced hunk mapping, line mapping, and variant allocations (`9ab0d65`, `e37260c`, `95c77a5`, `9d708cb`).
- Streamed/compacted conflict text and payload encoding/decoding while bounding retained memory (`583b14b`, `b71a162`, `0fa5802`, `357a04d`, `4b5962d`, `aeb5cc1`).
- Avoided caching stage blobs, reused validated caches, and opened the resolver without suspense (`75cb386`, `10c7e85`, `f5cf4c4`, `2e993ea`).

### Test and Verification Throughput

- Reused isolated Git repository templates and reduced repeated integration setup (`34f8447`, `eda24d1`).
- Added bounded process ownership, cancellation, and session-health gates (`74c0f12`, `089f559`).
- Kept the official backend suite below one minute without weakening assertions (`021c0ee`).
- Added application dependency-graph validation without launching the desktop host (`525a3c2`).

### Runtime Memory and Resource Lifetime

- Verified Settings, Bisect, and Force Push overlays plateau after warm-up rather than retaining linearly; a second 60-interaction pass added only 0.22 MB post-GC with no DOM growth (this commit).
- Verified Working Changes open/close cycles plateau after the status/cache warm-up; the second ten-cycle pass added only 0.08 MB post-GC with no DOM growth (this commit).
- Verified repeated native bisect-state reads do not leak process handles, and recorded the warmed four-tab host/WebView2 footprint as the next memory-reduction baseline (this commit).
- Diagnosed the warmed host with a managed heap dump: 74,804 BLite transactions were retained by the dependency registry, accounting for 14.96 MB of transaction, change-list, and dictionary-node objects plus a 1.86 MB expanded bucket table.
- Added an exact, concurrency-safe BLite transaction retention boundary to every application and cache write. Successful, failed, abandoned, and no-op operations now unregister only their own transaction; regression coverage proves the registry returns to zero (`this commit`).
- Batched each commit-graph page cache write into one caller-owned transaction instead of opening one auto-commit transaction per row (`this commit`).
- Verified 120 real WebView2 tab activations between the Chromium-scale repository and a disposable repository: small-repository content was ready in about 16 ms, Chromium was typically ready in 22-28 ms, and the worst sample was 37.6 ms.
- Verified multi-repository switching does not retain linearly: post-GC JavaScript heap increased only 0.36 MB across 120 activations while the active DOM count stayed constant; the on-disk app/cache databases remained bounded at 3 MiB and 5 MiB.
- Measured local-remote transport through the real WebView2 UI with an isolated bare remote: Fetch All and Pull (fast-forward only) each reflected in about 104 ms, and Push completed in about 96 ms.
- Verified fetch, pull, and push disable the transport toolbar during mutation, restore it afterward, refresh refs and ahead/behind state, and leave no page or console errors. Existing integration coverage protects diverged fast-forward rejection, force-with-lease safety, cancellation, and remote no-mutation on failure.
- Replaced lifetime-long retired pack-index and pack-file retention with generation-aware read leases. Fetch/repack can now replace native pack snapshots without interrupting active reads, while obsolete handles close after their final reader instead of accumulating until repository disposal (`986719d`).

### Clone and Remote Transport

- Verified a full public-network clone reports separate monotonic overall and current-phase progress through enumeration, counting, compression, receive, delta resolution, checkout, and completion; the 20,693-object fixture opened in 4.91 seconds.
- Verified active cancellation against an 824,255-object public remote reaches the canceling state in 6.2 seconds without freezing the WebView. The journey exposed a locked temporary pack that survived the old one-shot cleanup.
- Retry partial-clone cleanup after Git releases locked files, remove read-only files when required, and surface a specific error if the destination still cannot be reclaimed. The folder-name field now handles direct WebView input consistently with the URL and destination fields (`d795e85`).

## Rejected or Deferred Experiments

| Experiment | Observed benefit | Reason not shipped |
| --- | --- | --- |
| Enable Git FSMonitor for checkout | Chromium checkout improved from about 2.1 s to 0.85–0.90 s warm | Persistent daemon retained about 37.8 MB per repository and the first full status scan regressed to about 3.5 s. |
| Enable Git untracked cache automatically | Warm status reached about 0.41–0.44 s | Initial warm-up was about 3.6 s and mutating user repository configuration was not justified. |
| Suppress working-tree watchers during checkout | No repeatable improvement | Added lifecycle complexity without measurable value; reverted. |
| Swap Git executable path for checkout | No repeatable improvement | Nested app-process launch cost remained; reverted. |
| Replace standard stash push with `--quiet` or create/store/reset | None; warmed standard was 1.33 s, quiet 1.37 s, custom 1.47 s | Standard Git semantics were both fastest and safest, so the alternatives were rejected. |

## Next Measurement Areas

- Branch, tag, remote, stash, worktree, submodule, LFS, sparse checkout, bisect, rebase, merge, cherry-pick, revert, reset, and patch workflows.
- App startup, repository activation, tab switching, settings, command palette, dialogs, and every lazy overlay.
- Clone progress, authentication hand-off, cancellation, and large-transfer memory; repeat fetch/pull/push against a representative network remote where network variance can be isolated.
- Cold/warm cache invalidation cost and long-session resource scaling beyond the measured overlay, tab, handle, and transaction baselines.
