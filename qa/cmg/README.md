# LovelyGit CMG Smoke Suite

This suite runs against the real LovelyGit WebView2 app, not a browser-only Vite session. It launches through `scripts/Start-LovelyGitVisualTest.ps1`, attaches CMG to remote debugging port `9333`, and writes screenshots, GIFs, traces, and a JSON report under `artifacts/cmg/lovelygit-smoke/`.

## Preconditions

- `C:/CMG/CMG.exe` is installed.
- .NET and frontend build prerequisites from `AGENTS.md` are installed.
- LovelyGit has at least one known recent repository, or the app already opens to a selected repository. If the new-tab page has no recent rows, use the app once to add a local repository, then rerun the suite.

## Run

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-LovelyGitCmgSmoke.ps1
```

Use `-UseDotNetRun` when you want the same launch mode as the documented debug helper:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-LovelyGitCmgSmoke.ps1 -UseDotNetRun
```

## Coverage

The smoke pass validates:

- launch/attach to the visible WebView2 app;
- repository selection from current state or the first recent repository row;
- commit graph headers and screenshot evidence;
- commit row selection and details panel stats;
- working changes panel open and refresh;
- settings appearance dialog interaction;
- captured page-error and console-error gates after the risky UI flows.
