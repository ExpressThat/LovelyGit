# LovelyGit Workflow Documentation Gaps

This audit compares the current repository docs with the user-facing workflows
implemented in the app. The top-level `README.md` was previously empty and the
frontend README still contains the stock Vite template, so the main gap is not a
single stale paragraph; it is the absence of a maintained user workflow guide.

## Current Coverage

- `AGENTS.md` has useful contributor guidance for project shape, commands,
  frontend contracts, design-system rules, and CMG visual testing.
- `LovelyGit/Services/Git/Cli/GitCliServiceUsage.md` documents backend usage of
  `GitCliService`.
- `docs/team-task-planning.md` documents team task planning, not product usage.
- `LovelyGit/Frontend/README.md` is still the default Vite template and should
  not be treated as LovelyGit product or frontend architecture documentation.

## User Workflow Gaps

### Repository Selection

Current app behavior:

- The new-tab screen says "Open a repository" and offers `Open Repo`.
- Recent repositories can be searched by name or path.
- Selecting a recent repository sets `CurrentGitRepositoryId`.
- The top tab bar shows known repositories and supports repository switching.
- Repository actions include opening a terminal at the repository and revealing
  known repositories from top-nav controls.

Missing docs:

- No user-facing "open your first repository" flow explains that `Open Repo`
  opens a native folder picker and persists the repository in the recent list.
- No documented behavior for searching recent repositories, switching tabs, or
  removing/closing known repositories.
- No mention that the new-tab `Open Repo` button appears when no repository is
  selected, while repository tabs and controls appear once a repository is
  active.
- No documented troubleshooting for invalid/moved repository paths.

Proposed update:

- Add `docs/user-guide/repositories.md` with first-run, recent repository,
  repository switching, terminal/reveal actions, and stale-path behavior.

### Commit Graph Navigation

Current app behavior:

- The graph is virtualized and loads commit rows as the user scrolls.
- Columns are `Branch`, `Graph`, `Commit Message`, `Hash`, and `Author`.
- Column widths can be resized from the graph header.
- The refs panel lists branches, tags, stashes, and worktrees, and can be hidden
  or shown through the panel control or Graph View settings.
- Selecting a commit opens commit details in the sliding details panel.
- Commit details expose commit metadata, changed files, copy actions, and file
  diff views.

Missing docs:

- No user-facing explanation of graph columns, scrolling/loading behavior,
  selected commit behavior, or the horizontal graph lane scroller.
- No docs for refs panel filtering or using branches/tags/stashes/worktrees to
  jump to commits.
- No docs for commit details, copy actions, changed-file selection, or how diff
  settings affect commit and working-tree file diffs.
- No documented expectation for large repositories, cache warm-up, or transient
  loading rows.

Proposed update:

- Add `docs/user-guide/commit-graph.md` covering graph layout, refs panel,
  commit selection, details panel, changed files, and expected loading states.

### Working Changes

Current app behavior:

- The top nav has a `Working changes` action with a count badge.
- The working changes panel lists staged, unstaged, untracked, and unmerged
  files.
- Users can stage/unstage selected or all files, discard changes through a
  confirmation dialog, select files for working-tree diffs, and commit staged
  changes with title/body fields.

Missing docs:

- No user-facing guide explains the working changes badge, staging commands,
  discard confirmation, unmerged files, file diff selection, or commit form.
- No documented behavior after successful commits, such as refreshing the graph.
- No warning that discard operations are destructive.

Proposed update:

- Add `docs/user-guide/working-changes.md` with staging, unstaging, discard,
  diff, and commit workflows.

### Settings And Theme Behavior

Current app behavior:

- Settings categories are `Appearance`, `File Diff View`, `Graph View`, and
  `Remote Operations`.
- Appearance supports System/Light/Dark modes, paired light/dark palettes,
  accent/background/foreground overrides, UI fonts, and code fonts.
- File Diff View controls side-by-side vs combined layout, changed hunks vs full
  file, context lines, wrapping, and whitespace-only change handling.
- Graph View controls whether the refs panel is shown.
- Remote Operations controls the default fetch/pull toolbar action.
- Settings are persisted in `LovelyGit.blite` and applied before React renders
  to avoid a launch flash.

Missing docs:

- No user-facing settings reference exists.
- No docs clarify that user accent choices drive primary emphasis, while the
  shadcn `accent` interaction surface remains background-derived.
- No docs explain that diff settings affect both commit diffs and working-tree
  file diffs.
- No docs explain how System mode chooses between the configured light and dark
  palettes.
- No troubleshooting for resetting bad custom colors/fonts or locating the
  settings database.

Proposed update:

- Add `docs/user-guide/settings.md` with settings categories, persistence,
  theme-mode selection, palette overrides, font behavior, and reset guidance.
- Add a short contributor note near the design-system section of `AGENTS.md`
  pointing future theme changes at the user-facing settings guide once it
  exists.

### CMG Visual Testing

Current docs:

- `AGENTS.md` has a strong CMG checklist for launching the native WebView2 app,
  attaching to port `9333`, driving controls, saving screenshots/GIFs, checking
  diagnostics, and using accessibility snapshots.

Missing docs:

- CMG expectations are contributor-facing only and long enough that they are
  hard to cite from PR handoffs.
- No short test recipe maps common workflows to evidence artifacts.
- No sample CMG scripts exist under `artifacts/` or `docs/` for repository
  selection, graph navigation, settings/theme changes, or working changes.
- No visual testing matrix names the minimum surfaces to inspect for theme
  changes beyond the detailed `AGENTS.md` bullet.

Proposed update:

- Add `docs/qa/cmg-visual-testing.md` as a concise PR handoff recipe linking
  back to `AGENTS.md`.
- Add reusable CMG script examples for:
  - attach and screenshot the active repository graph;
  - open settings and capture Appearance;
  - toggle refs panel and verify no console/page errors;
  - capture accessibility snapshots for settings dialogs or custom controls.

## Recommended Documentation Plan

1. Replace the stock `LovelyGit/Frontend/README.md` with frontend-specific setup,
   scripts, generated contract notes, and testing commands.
2. Add a small `docs/user-guide/` set for repository selection, commit graph,
   working changes, and settings.
3. Add `docs/qa/cmg-visual-testing.md` as the short visual QA handoff doc and
   keep `AGENTS.md` as the detailed contract.
4. Keep the top-level `README.md` as the entry point to user, contributor, and
   QA docs instead of duplicating detailed workflow content.

## Verification Notes

- This audit was performed by reading repository Markdown files and the current
  React components for the workflows above.
- No runtime behavior was changed.
- No CMG run is required for this documentation-only audit.
