# LovelyGit product roadmap and feature inventory

This document makes the continuous product program visible. It is a planning
map, not a release promise: keep it current as capabilities land, move, or are
cut from scope.

## Current product inventory

LovelyGit is a desktop Git client focused on repository navigation, commit
history review, working-tree review, and common repository actions from a native
WebView2 shell. The current user-facing workflow details live in
[workflows.md](workflows.md).

Implemented product areas:

- Repository onboarding: open a local Git repository, reopen recent known
  repositories, switch repository tabs, remove known repositories, reveal a
  repository folder, and open a terminal at the repository path.
- Commit graph: page through large histories, render graph lanes, show local
  refs, remote refs, tags, stashes, and worktree context, and refresh after
  mutating actions.
- Commit review: select commits, inspect changed files, open file diffs, show
  patch text where available, copy commit details, and build remote commit URLs.
- File diff review: combined, side-by-side, compact, and virtual text diff
  surfaces for commit and working-tree files.
- Working tree: load staged, unstaged, untracked, and unmerged file summaries;
  inspect file diffs; stage and unstage files; stage and unstage individual diff
  lines; discard changes; reveal working-tree files; and commit staged changes.
- Remote and branch actions: fetch, pull, push, checkout branch, create branch,
  delete branch, and rename branch.
- Settings: appearance, theme, font, graph display, file/diff display, and
  remote-action behavior, with settings applied before React renders and updated
  live.
- App foundations: typed native-message contracts, generated frontend types,
  persistent app settings, transient commit graph cache, bundled Git support,
  WebView2 visual QA flows, and baseline performance evidence.

## Missing high-value Git GUI workflows

These gaps are ordered by user value and by how much they unlock daily Git GUI
use without leaving the app.

| Priority | Workflow | Why it matters | First useful slice |
| --- | --- | --- | --- |
| P0 | Reliable review gates | Product work needs a green baseline before larger workflow expansion. | Restore .NET test compilation, keep frontend tests green, and make selected-repository CMG checks repeatable on a fixture repository. |
| P0 | Commit details reliability and latency | Commit selection is core to the graph experience and is currently a baseline blocker. | Fix or clarify the click-to-details path, add timing probes, and enforce warm/cold details targets from [performance-baseline.md](performance-baseline.md). |
| P1 | Merge conflict resolution | Conflict handling is a best-in-class Git GUI expectation and completes pull/merge workflows. | Detect unmerged paths, group conflict entries, show ours/theirs/base status, and support choose-ours/choose-theirs/mark-resolved on disposable fixtures. |
| P1 | Stash workflows | Users need to shelve work before checkout, pull, review, or experimentation. | List stashes, create stash with message, apply, pop, drop, and view stash diffs. |
| P1 | Branch comparison and history filters | Review workflows need targeted history, not only the full graph. | Add branch/tag/search filters, ahead/behind summaries, and compare current branch against another ref. |
| P1 | Revert and cherry-pick | Common code-review and release-management workflows need commit-level mutations. | Add single-commit revert and cherry-pick with preflight dirty-tree checks and clear conflict handoff. |
| P2 | Tag management | Release workflows need lightweight and annotated tag support. | Create, delete, push, and inspect tags from graph/ref actions. |
| P2 | Rebase and reset | Advanced branch cleanup is expected in mature Git clients but carries higher risk. | Add guarded reset modes first, then interactive rebase planning with explicit recovery guidance. |
| P2 | Clone and remote management | First-run onboarding should not require an existing local repository. | Clone repository by URL into a chosen folder, then list/add/remove remotes and set upstream branch. |
| P2 | Search and command palette | Dense desktop workflows need fast navigation across repos, commits, refs, files, and actions. | Add command palette for app actions, ref/commit lookup, and file-path search in current diffs. |
| P3 | Blame, file history, and workspace preview | Deep code investigation benefits from file-centric history. | Show file history from a selected path and open blame for a committed or working-tree file. |

## Priority slices

Use these slices to keep feature work small enough to review and verify.

1. Stabilize the baseline: restore .NET tests, preserve frontend tests, keep
   docs current, and turn the commit-details blocker into a measured gate.
2. Harden the daily loop: graph selection, commit details, file diffs,
   working-tree staging, commit, fetch, pull, push, checkout, and branch actions.
3. Add missing daily Git GUI workflows: merge conflict resolution, stash
   management, branch comparison, history filters, revert, and cherry-pick.
4. Extend release and advanced workflows: tag management, guarded reset, rebase,
   clone, remote management, and upstream tracking.
5. Improve navigation and investigation: command palette, commit/ref/file
   search, blame, file history, saved workspace state, and richer copy/export
   affordances.
6. Raise product quality continuously: accessibility checks, theme coverage,
   performance instrumentation, bundled-Git reliability, release notes, and
   contributor docs.

## Lane ownership patterns

Each roadmap item should name one primary lane and the supporting lanes it needs
before implementation starts.

| Lane | Owns | Typical outputs |
| --- | --- | --- |
| Feature | Backend command contracts, Git operation behavior, frontend workflows, state handling, and failure modes. | Native-message contract changes, responders, generated TypeScript types, UI integration, focused tests. |
| Design | Information architecture, visual hierarchy, interaction states, accessibility names, empty states, and destructive-action confirmation. | Screenshots, CMG accessibility snapshots, reviewed copy, keyboard and focus behavior notes. |
| Performance | Startup, graph paging, details loading, diff rendering, long-task behavior, bundle size, and memory growth. | Instrumentation, before/after timings, profiles, memory samples, updated baseline rows. |
| QA | Fixture repositories, repeatable CMG flows, regression scripts, diagnostics gates, and destructive-flow safety. | Reusable scripts under `scripts/cmg/`, reports/traces/GIFs/screenshots under `artifacts/cmg-qa/`. |
| Docs | Product workflow docs, roadmap updates, contributor workflow notes, and evidence pointers. | Docs-only PRs or docs commits that describe shipped behavior and link to active feature work without depending on it. |
| Review | Scope control, risk checks, test coverage, merge guidance, and follow-up task creation. | Review comments tied to files, task comments, merge-readiness notes, and explicit known gaps. |

## Definition of done

The done bar scales with risk, but each product slice should explicitly answer
these points before merge:

- Behavior: the user workflow is described in [workflows.md](workflows.md) when
  it changes current product behavior.
- Contracts: backend native-message contracts, JSON source generation,
  responders, frontend generated types, and callers are kept in sync.
- Tests: run the focused unit/build command that covers the touched area; if a
  known blocker prevents a command, state that blocker and link to the baseline
  or task tracking it.
- Visual QA: user-visible changes are checked in the real WebView2 app with CMG
  using the conventions in
  [scripts/cmg/lovelygit-qa/README.md](../scripts/cmg/lovelygit-qa/README.md).
- Performance: graph, diff, details, startup, settings, or repository-switch
  changes either stay within [performance-baseline.md](performance-baseline.md)
  gates or update the baseline with evidence and rationale.
- Accessibility: dialogs, custom controls, menus, and dense panels have stable
  accessible names and focus behavior validated by CMG locators or snapshots.
- Safety: mutating Git actions are verified against disposable repositories and
  include preflight handling for dirty state, conflicts, missing refs, and
  command failures.
- Evidence: screenshots, GIFs, traces, reports, profiles, and exploratory
  scripts are saved under `artifacts/`; reusable flows move to `scripts/cmg/`.

## Evidence conventions

Use evidence to make review decisions repeatable:

- Prefer real WebView2 app evidence over browser-only Vite sessions for product
  behavior.
- Include the repository fixture or real repository path used for graph,
  working-tree, and performance evidence.
- Pair screenshots or GIFs with diagnostics gates for page errors and console
  errors after risky interactions.
- For performance claims, include the command or CMG script, before/after
  numbers, process memory when relevant, and any known environmental caveats.
- For destructive Git actions, state whether the run used a disposable fixture
  and what cleanup or reset was performed.

## Keeping docs independent from active feature PRs

Docs should guide planning without becoming blocked by feature branches.

- Roadmap and inventory updates may land independently when they describe current
  reality, planned priorities, or accepted gaps.
- Feature PRs should update workflow docs only for behavior that is implemented
  in that PR. Planned behavior belongs here or in a task comment, not in shipped
  workflow docs.
- Docs-only PRs should avoid generated files, app assets, and unrelated code
  churn.
- If a feature PR needs future docs, leave a narrow follow-up task or roadmap
  note rather than broad speculative text.
- Keep links stable: product behavior links to [workflows.md](workflows.md), QA
  evidence links to the CMG QA README, and performance claims link to
  [performance-baseline.md](performance-baseline.md).
