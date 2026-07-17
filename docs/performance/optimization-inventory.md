# Completed Optimization Inventory

This ledger records shipped performance work by feature area. Update it in the same commit as each new optimization.


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
- Streamed large stash reflogs from a pooled buffer and assigned newest-first selectors in place instead of retaining a second full raw-line representation (`f2d0af1b`).
- Scanned primary remote URLs with a pooled purpose-built reader instead of constructing the complete remote model during repository refreshes (`1c2629b`).
- Read tiny worktree `HEAD`, `gitdir`, and lock files through stack buffers while preserving the existing bounded concurrency (`48a56ad`).
- Reset scroll on tab switches and reduced virtualized graph overscan (`6b0fcf6`, `ca11dfd`, `7180d47`).

### Native Git Parser and Object Storage

- Accelerated packed commit reads, shared a bounded object cache, and supported alternate object stores (`a3ecfc4`, `c8e17b4`, `bedf3b6`).
- Avoided opening object stores without upstreams and avoided search pollution of the object cache (`93dde92`, `f4c3080`).
- Read active bisect state from direct HEAD/object data and the worktree-scoped `refs/bisect` directory instead of loading every repository ref twice (`ff73534`).
- Started bisect by resolving only the worktree-aware HEAD and selected known-good commit object instead of materializing the complete repository/ref model (`037f147`).
- Built interactive-rebase plans from direct HEAD and uncached commit-object reads, parsing each commit once without materializing the repository's complete ref model (`9434fa4`).
- Validated detached checkout targets through the direct commit-existence reader instead of opening the complete repository/ref model (`de7c3b4`).
- Compared authoritative commit pairs through an object-database-only repository session, keeping unrelated refs out of deep-history comparisons (`44344c2`).
- Resolved named branch comparisons through worktree-aware HEAD plus the exact selected local/remote ref before opening an object-only session, preserving local-first precedence without enumerating unrelated refs (`b6816f7`).
- Converted binary SHA-1/SHA-256 identities through a stack character buffer instead of allocating an intermediate `char[]` before the result string (`bb89ffc`).
- Demand-read native monolithic and split commit-graph chains for ancestry painting instead of inflating every packed commit; the reader stays unopened for unrelated repository features and safely falls back for missing, corrupt, shallow, grafted, replaced, or disabled histories (`dfb379a`).
- Reused native commit-graph ancestry in deep file-history and blame walks while preserving authoritative tree, blob, author, rename, and attribution reads (`3ca172f`).
- Opened history and blame against the object database alone and resolved only the worktree-scoped HEAD when no explicit start was supplied, so unrelated refs no longer affect either interaction (`391cd62`).
- Read submodule status from the parent HEAD/tree plus each initialized submodule's worktree-aware HEAD, avoiding complete parent and nested ref models (`7c4539a`).
- Read pull/push sync status from the worktree-aware HEAD and exact configured upstream ref, then traverse only the object database; unrelated refs no longer affect toolbar refresh latency or allocations (`0f6f3ea`).
- Streamed large Git configs through a pooled line reader for object-format discovery and current-branch upstream lookup, while retaining a stack-buffered fast path for normal small configs; unrelated configured branches no longer inflate toolbar refresh memory (`e5d203a`).
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
- Streamed sparse-checkout specifications directly into the response collector instead of retaining a duplicate raw-line array (`d70f301`).
- Streamed large `.gitattributes` files through a pooled parser so non-LFS lines do not become managed strings while retaining the existing bounded result (`145b479`).
- Streamed Git config and ignore sources through pooled line buffers and matched literal ignore rules directly instead of compiling one regex per rule, sharply reducing initial status GC pressure while retaining regex handling for actual glob rules (`6fed8fa`).
- Matched common single-leading-asterisk suffix rules such as `*.pdb` directly per path segment, preserving compiled-regex fallback for complex globs while removing their dominant large-rule-set latency and memory cost (`this commit`).

### Diff Engine and File Diffs

- Replaced DiffPlex with the single low-allocation diff engine (`a1625fc`).
- Trimmed unchanged edges, vectorized line preparation, reduced line splitting/context allocations, and accelerated ignored-whitespace diffs (`91de674`, `5ca8e3e`, `9ee5854`, `470bc1c`, `5890323`).
- Virtualized and bounded large/long-line rendering and halved side-by-side row work (`30404a9`, `4dd7b33`, `6c4a42b`, `4429b34`, `df83dfe`).
- Compacted, compressed, referenced, and reused large colored diff payloads across layouts (`b55def8`, `f4f6d53`, `46af1fd`, `80d6769`, `ac7fb8a`, `df0c41b`, `d10ac7b`).
- Bounded decoded/source/result cache weight and skipped uncacheable prewarming (`15ce59d`, `9f613ed`, `c4e610c`, `70eecfd`).
- Moved diff persistence off click paths and cached open variants (`b074af7`, `dc120ec`, `54cb0c7`, `3726952`).
- Streamed patch previews through a pooled character buffer so large Apply Patch files no longer allocate one string per content line (`dfe357d`).
- Opened commit and series patch export against the object database alone, avoiding unrelated branch, remote, tag, and stash enumeration (`9b71ea5`).
- Opened commit-file diff sources against the object database alone, keeping unrelated refs out of cold file inspection while preserving cached Side-by-Side, Combined, whitespace, and parent variants (`3d932ef`).

### Conflict Resolution

- Virtualized large outputs/gutters and accelerated display changes (`c921509`, `0a25e71`, `d955b50`, `13635ca`).
- Reused worktree snapshots, source lines, prepared models, view variants, and unchanged text (`a204ebf`, `cf67ef4`, `0cdb614`, `1880b11`, `11e8732`).
- Reduced hunk mapping, line mapping, and variant allocations (`9ab0d65`, `e37260c`, `95c77a5`, `9d708cb`).
- Streamed/compacted conflict text and payload encoding/decoding while bounding retained memory (`583b14b`, `b71a162`, `0fa5802`, `357a04d`, `4b5962d`, `aeb5cc1`).
- Avoided caching stage blobs, reused validated caches, and opened the resolver without suspense (`75cb386`, `10c7e85`, `f5cf4c4`, `2e993ea`).
- Replaced repeated line-by-hunk scans during Changes/Full-file switching with a precedence-preserving interval index (`42fc630`).
- Checked external merge-tool preflight/postflight state through the exact worktree index path instead of loading all refs and index entries; this also corrected linked-worktree conflict detection (`b91af35`).

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

