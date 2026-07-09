# LovelyGit workflows

This document describes the current user-facing workflows and the verification
expectations contributors should use when changing them.

## Product shape

LovelyGit is a desktop Git client. The app window hosts a React frontend through
WebView2, while the ASP.NET Core backend executes repository commands and serves
typed native-message responses. Users work from either the new-tab repository
picker or the selected-repository view.

Local app data is stored under `%LOCALAPPDATA%/LovelyGit/`. The persistent app
database is `LovelyGit.blite`; the transient commit graph cache is
`LovelyGit.Cache.blite` and is cleared on app startup.

## Repository selection

When no repository is selected, the app shows the new-tab view:

- `Open Repo` launches the native folder picker for adding a Git repository from
  disk.
- Recent repositories are listed so users can reopen an existing known
  repository.
- Repository tabs in the top bar let users switch between known repositories.

Known repository actions also support removing a repository from the list,
revealing the repository folder in the platform file manager, and opening a
terminal at the repository path.

## Selected-repository view

After a repository is selected, the main workspace shows the commit graph. The
top bar displays the current branch name, remote actions, terminal action,
working-changes entry point, settings, and repository tabs.

The commit graph workflow supports:

- Paged graph loading for large repositories.
- Local refs, remote refs, tags, stashes, and worktree context in the graph UI.
- Commit selection, which opens the sliding `Commit Details` panel.
- Commit file selection, which opens a full diff view for the selected file.
- Patch viewing for commits that expose patch text.
- Repository refresh after mutating actions.

Branch and ref workflows currently include checkout, branch creation, branch
rename, and branch deletion from supported ref UI actions. Remote workflows
include fetch, pull, and push.

## Working-tree workflow

The working-changes button opens the sliding `Working Changes` panel for the
current repository. The badge shows the current total change count when changes
are present.

The working-tree workflow supports:

- Summary and full-list loading for staged, unstaged, untracked, and unmerged
  files.
- File-level diff review for working-tree changes.
- Staging and unstaging selected files.
- Staging and unstaging individual diff lines when the diff view supports it.
- Discarding working-tree changes.
- Committing staged changes with a commit message.

Discard and commit actions mutate the repository. Validate them against a
disposable fixture repository unless the test explicitly requires a real working
repository.

## Settings workflow

The settings dialog controls user preferences that are loaded before React
renders and updated live while the app is running. Current settings areas cover
appearance, graph display, file/diff display, and remote-action behavior.

When changing theme, font, or settings bootstrap behavior, visually verify both
startup and live changes. The app should not flash the wrong theme or font on
launch.

## Contributor workflow

For a normal app change:

1. Keep native-message contracts, backend responders, JSON source-generation,
   generated frontend types, and frontend callers in sync.
2. Regenerate frontend contracts with `pnpm generate:csharp-types` when backend
   command, setting, or native-message types change.
3. Run the focused build or test command that covers the changed area.
4. Use the real WebView2 app and CMG for user-visible workflow changes.
5. Save reusable CMG scripts under `scripts/cmg/`; save one-off evidence under
   `artifacts/`.

For docs-only changes, a diff review is usually enough unless the docs update
claims a command, UI label, or workflow behavior that should be verified against
the current app.

## QA evidence conventions

For visual workflow gates, collect evidence from the real desktop app:

- Launch through `scripts/Start-LovelyGitVisualTest.ps1`.
- Attach CMG to WebView2 remote debugging on port `9333`.
- Use reusable scripts from `scripts/cmg/lovelygit-qa/` when they fit the
  scenario.
- Save screenshots, GIFs, structured reports, traces, and accessibility snapshots
  under `artifacts/cmg-qa/`.
- After risky UI actions, assert no captured page errors or console errors.

Use the no-repository startup flow for repository picker and settings smoke
coverage. Use the selected-repository flow for graph, commit details, working
changes, settings, accessibility, and diagnostics coverage. Use mutating
working-tree flows only on disposable repositories.

## Known verification gaps

The current docs and baseline intentionally call out gaps instead of treating
them as passing gates:

- The performance baseline notes that .NET test execution is compile-blocked on
  the current PR head.
- Commit-details latency is not yet enforceable until the details opening flow is
  clarified or fixed in the baseline scenario.
- Large-repository performance gates still need lightweight instrumentation
  around app startup, native-message timing, graph paging, details loading, and
  backend command responders.
