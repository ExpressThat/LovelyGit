# LovelyGit CMG QA flows

Repeatable CMG coverage for LovelyGit's real WebView2 desktop app. These flows cover startup, selected-repository behavior, visual polish, accessibility, and frontend diagnostics.

## Launch and attach

Run from the repository root. Use this checkout's helper script; the absolute `C:/Projects/LovelyGit/...` path from `AGENTS.md` is equivalent when you are in the main checkout.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Start-LovelyGitVisualTest.ps1 -UseDotNetRun -Width 1600 -Height 1000
Invoke-WebRequest -UseBasicParsing http://127.0.0.1:9333/json
C:/CMG/CMG.exe browser app attach --port 9333
```

The WebView2 target should be titled `LovelyGit` and use `http://localhost:5000/`. Keep the native app window visible while the flows run.

## Flow set

`startup-no-repo.cmgscript`
: Runs from a new-tab/no-current-repository state. It checks startup UI, the `Open Repo` affordance, settings dialog accessibility, screenshots, and console/page-error gates.

`repository-workflow.cmgscript`
: Runs after a repository is selected. It checks commit graph headers and rows, paging/scrolling, commit selection, commit details, settings/theme/dialog/dropdown behavior, working changes panel, screenshots, accessibility snapshots, and diagnostics. The commit-row click uses the current generic row button shape because commit rows do not yet expose a stable generic accessible name.

`working-tree-mutating-flow.cmgscript`
: Optional destructive flow for a disposable fixture repository only. It checks stage/unstage affordances when working-tree changes are available. Do not run it on a real working repo.

## Recommended command sequence

For a clean startup/no-repo pass with structured evidence:

```powershell
C:/CMG/CMG.exe run scripts/cmg/lovelygit-qa/startup-no-repo.cmgscript --browser-port 9333 --report-json artifacts/cmg-qa/startup-no-repo.report.json --trace artifacts/cmg-qa/startup-no-repo-trace --gif artifacts/cmg-qa/gifs
```

For selected-repository coverage with structured evidence:

```powershell
C:/CMG/CMG.exe run scripts/cmg/lovelygit-qa/repository-workflow.cmgscript --browser-port 9333 --report-json artifacts/cmg-qa/repository-workflow.report.json --trace artifacts/cmg-qa/repository-workflow-trace --gif artifacts/cmg-qa/gifs
```

For the optional disposable-repository mutation pass:

```powershell
C:/CMG/CMG.exe run scripts/cmg/lovelygit-qa/working-tree-mutating-flow.cmgscript --browser-port 9333 --report-json artifacts/cmg-qa/working-tree-mutating-flow.report.json --trace artifacts/cmg-qa/working-tree-mutating-flow-trace --gif artifacts/cmg-qa/gifs
```

## Setup notes

- To test add/select repo end to end, start on `startup-no-repo`, click `Open Repo`, and choose a repository in the native folder picker manually. CMG cannot reliably drive the native picker from the WebView2 page target. After the repository is selected, run `repository-workflow.cmgscript`.
- Use a large visible window for the repository workflow; dense panels and diffs need room for visual review.
- Save all evidence under `artifacts/cmg-qa/`: screenshots, GIFs, accessibility snapshots, JSON reports, and traces.
- After every risky UI interaction, the scripts list and assert no page errors or console errors. CMG diagnostics are forward-only, so attach before reproducing failures.
- Run the optional mutating flow only against a disposable fixture repository with known unstaged changes.
- Leave temporary exploratory scripts in `artifacts/` unless they are generalized enough to reuse from this directory.
