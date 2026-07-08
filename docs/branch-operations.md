# Branch Operations Workstream

LovelyGit's next branch/ref feature slice is focused on making the existing refs
panel and top bar actionable while preserving the typed native-message command
flow between the React frontend and the .NET backend.

## User Behavior

- Checkout should be available from local branch rows in the refs panel.
- Remote branch rows should support creating and checking out a sensible local
  tracking branch when the branch name can be derived safely.
- Users should be able to create a branch from the selected commit or from
  `HEAD`, with an option to checkout the new branch immediately.
- Local branches should support rename and delete actions when valid.
- Deleting a branch must require confirmation, must not allow deleting the
  current branch, and should require an explicit force path for unmerged
  branches.
- Successful mutations should refresh refs, graph rows, the current branch
  label, and working-tree state without restarting the app.

Non-goals for this slice are merge/rebase conflict workflows, branch comparison
views, full upstream editing, and tag management beyond existing display.

## Developer Notes

Branch actions follow the current command pattern:

- Command entries live in `NativeMessageType`.
- Branch command responders are implemented under
  `Services/NativeMessaging/CommandResolvers/WorkingTree`.
- Responders are registered through the working-tree service collection
  extension.
- Argument types are included in the source-generated JSON context.
- Frontend contracts must be regenerated with `pnpm generate:csharp-types`
  after contract changes; do not hand-edit
  `LovelyGit/Frontend/src/generated/**`.
- Call Git through `GitCliService`/`GitOperationService` and keep command output
  bounded.
- Keep branch-name validation server-side even when the UI validates early.

Implemented command coverage:

- Checkout local branch.
- Checkout remote branch as a local tracking branch.
- Create branch from commit or `HEAD`.
- Rename local branch.
- Delete local branch, with a force flag for unmerged branches.

## Performance Notes

- Keep mutation responses small and trigger one debounced refresh path after
  success.
- Avoid per-row backend calls from the refs panel.
- Reuse existing ref summary readers for display metadata instead of parsing
  verbose branch output on every render.
- Do not cache branch histories or large Git command output for this feature.
- Measure interaction latency for opening action menus/dialogs, Git operation
  duration for branch mutations, post-mutation refs/graph refresh latency, and
  process memory before/after repeated branch actions.

Initial budget targets should be confirmed by QA/performance work. Until then,
this document records expected checks rather than measured results.

## Verification Notes

Minimum build checks:

```powershell
dotnet build LovelyGit/LovelyGit.csproj
```

When backend contracts change:

```powershell
Set-Location LovelyGit/Frontend
pnpm generate:csharp-types
pnpm lint
```

Real-app visual checks must use the CMG/WebView2 flow from `AGENTS.md`, not a
plain browser-only localhost session. Capture screenshots or GIFs under
`artifacts/` and inspect diagnostics after risky UI actions:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File C:/Projects/LovelyGit/scripts/Start-LovelyGitVisualTest.ps1
C:/CMG/CMG.exe browser app attach --port 9333
C:/CMG/CMG.exe browser control events pageErrors expectNoPageError --timeout 250
C:/CMG/CMG.exe browser control events console expectNoConsole --level error --timeout 250
```

Visual coverage should include the refs panel, branch action menu, create,
rename, and delete dialogs, checkout success state, top bar current-branch
refresh, and at least one light and one dark custom theme.

## Current Status

- Implementation commit: `ffa894f4 feat: add branch ref operations`.
- Docs ownership: this document covers the branch/ref operation feature notes.
  Definition-of-done/QA process docs are owned by the separate Alice docs lane.
- QA artifacts and CMG screenshots are tracked by the QA lane.
- Performance measurements are not yet recorded; the performance notes above
  identify expected measurement points for follow-up QA.
