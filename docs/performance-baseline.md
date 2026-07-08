# LovelyGit performance baseline

Baseline captured on 2026-07-08 from the real WebView2 desktop app using CMG against `C:\Projects\chromium-tessting` as the selected repository. The app was launched with `scripts/Start-LovelyGitVisualTest.ps1 -Width 1440 -Height 1000`, attached through WebView2 remote debugging on port `9333`, and exercised with scripts saved under `artifacts/`.

## Current baseline

| Area | Baseline | Target for merge gate | Notes |
| --- | ---: | ---: | --- |
| Frontend production build | 9.8 s | No regression > 15% unless explained | Emits a large chunk warning; startup work should include bundle tracking. |
| Backend build | 2.4 s | Keep green | `dotnet build LovelyGit/LovelyGit.csproj --verbosity quiet`. |
| .NET tests | 2.3 s | Keep green | `dotnet test LovelyGit.Tests/LovelyGit.Tests.csproj --verbosity quiet`. |
| Frontend tests | 2.1 s, failing | Must pass | Current failure: `bootstrapApp.test.tsx` mock lacks `getSetting`. Before generating contracts, tests also fail because `src/generated/native-message-contracts` is absent. |
| Startup DOM loaded | 261 ms | p95 <= 750 ms | From `performance.getEntriesByType('navigation')[0].domContentLoadedEventEnd`. |
| Startup first contentful paint | 2.876 s | p95 <= 2.5 s, hard fail > 3.5 s | Warm visible launch; target should be validated over at least 5 cold-ish launches. |
| Startup usable graph | Graph visible after attach; 110 row/control nodes sampled | p95 <= 3.0 s to visible graph/new-tab UI | Use visible app assertion: `COMMIT MESSAGE` or `Open Repo` depending selected repository state. |
| Commit graph first page | 400 row page size; visible graph loaded with Chromium repo | p95 <= 1.5 s for first page | Needs explicit app instrumentation around `CommitGraph` SignalR request/response to separate backend and render cost. |
| Commit graph scroll/paging | 2400 px scroll, target lower row visible in 92.8 ms | p95 <= 120 ms per page jump; no long task > 50 ms | One 56 ms long task observed, so this is already near the bar. |
| Commit details | UI baseline blocked | p95 <= 350 ms warm cache, <= 900 ms cold commit | CMG user click selected/no-op did not open `Commit Details`; must be fixed or clarified before details latency can be enforced. |
| Settings dialog open | 119.8 ms | p95 <= 200 ms | `getByTitle=Settings` to `Appearance` visible. |
| Theme setting interaction | Light/Morning applied by 241.5 ms from probe start | p95 <= 250 ms; no long task > 50 ms | No new long task during settings interaction in this run. |
| Native app memory | 169.8 MiB WS / 116.0 MiB private after launch; 205.9 MiB WS / 152.7 MiB private after graph scroll + settings | Growth <= 75 MiB for 2k row traversal; no unbounded growth across repeated traversals | Process `LovelyGit` PID 10772. |
| WebView2 memory | Largest WebView2 WS about 135.7 MiB after launch; about 142.0 MiB after interactions | Track total WebView2 private bytes; investigate > 150 MiB growth per traversal scenario | Multiple WebView2 helper processes are expected. |
| Frontend diagnostics | No captured page errors or console errors | Hard fail on any page error or console error | CMG diagnostics passed after graph/settings probes. |

## Measurement commands

Use the real desktop app, not a browser-only Vite session:

```powershell
pnpm --dir LovelyGit/Frontend prod
powershell -NoProfile -ExecutionPolicy Bypass -File C:/Projects/LovelyGit/scripts/Start-LovelyGitVisualTest.ps1 -Width 1440 -Height 1000
Invoke-WebRequest -UseBasicParsing http://127.0.0.1:9333/json
C:/CMG/CMG.exe browser app attach --port 9333
C:/CMG/CMG.exe browser --port 9333 control script --file artifacts/lovelygit-baseline-startup.cmgscript --trace artifacts/lovelygit-baseline-startup-trace.json
C:/CMG/CMG.exe browser --port 9333 control script --file artifacts/lovelygit-baseline-scroll-only.cmgscript --trace artifacts/lovelygit-baseline-scroll-only-trace.json
C:/CMG/CMG.exe browser --port 9333 control script --file artifacts/lovelygit-baseline-settings.cmgscript --trace artifacts/lovelygit-baseline-settings-trace.json
C:/CMG/CMG.exe browser --port 9333 control events pageErrors expectNoPageError --timeout 250
C:/CMG/CMG.exe browser --port 9333 control events console expectNoConsole --level error --timeout 250
```

Build and test gates:

```powershell
Measure-Command { dotnet build LovelyGit/LovelyGit.csproj --verbosity quiet }
Measure-Command { dotnet test LovelyGit.Tests/LovelyGit.Tests.csproj --verbosity quiet }
Measure-Command { pnpm --dir LovelyGit/Frontend test }
```

Memory sampling:

```powershell
Get-Process -Id <LovelyGitPid> | Select-Object Id,ProcessName,WorkingSet64,PrivateMemorySize64,CPU
Get-Process msedgewebview2 | Select-Object Id,ProcessName,WorkingSet64,PrivateMemorySize64,CPU | Sort-Object WorkingSet64 -Descending | Select-Object -First 8
```

## Required instrumentation

Add lightweight timing around these boundaries before making #f50c1ab2 enforceable:

- `bootstrapApp`: settings initialization start/end, pre-React theme/font apply, first React render marker.
- Native messaging transport: command send, response received, success/failure, payload size for `CommitGraph`, `GetCommitDetails`, `GetAllSettings`, `SetSetting`, and `SetMultipleSettings`.
- `useCommitGraphData`: first request start, first page applied, additional page applied, row count, cursor state.
- `CommitDetails`: selected row timestamp, request start, response received, loaded state committed.
- Graph viewport: scroll start/end, requested row range, time until requested rows visible.
- Backend command responders: `Stopwatch` around repository open, refs load, graph page construction, commit details build, diff/cache reads, and settings persistence.

## Profiling required before merge

Require a CPU or memory profile before merging any change that meets one of these conditions:

- Changes commit graph traversal, row construction, lane layout, pack/object parsing, refs loading, or graph cache persistence.
- Changes `useCommitGraphData`, virtualized graph rendering, row/cell components, refs panel rendering, or details panel rendering.
- Adds synchronous frontend work that can run during startup, repository switch, graph scroll, commit selection, or settings/theme updates.
- Increases production JS bundle size enough to trigger or worsen the current Vite chunk warning.
- Raises startup FCP, graph first page, graph scroll, details load, or settings interaction by more than 15% from this baseline.
- Adds memory-retaining caches or subscriptions in graph, details, working tree, settings, or native messaging paths.

Profiles should include a CMG screenshot or GIF, browser long-task data, command timing logs, and before/after process memory. For graph changes, collect at least one large repository run and one repeat traversal run to catch retained state.
