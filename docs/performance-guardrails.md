# Performance Guardrails

LovelyGit has a repeatable local guardrail for the commit graph and commit details path. Run it before PRs that touch startup, repository selection, commit graph paging, graph row rendering, commit details, native messaging, git parsing, or cache behavior.

## Prerequisites

- CMG is installed at `C:\CMG\CMG.exe`.
- The target repository has already been added to LovelyGit and is the current selected repository.
- Build prerequisites from `AGENTS.md` are installed.

The guardrail uses the real WebView2 desktop app. It does not drive the folder picker, so repository setup is intentionally a one-time manual step.

## Run

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Measure-LovelyGitPerformanceGuardrails.ps1
```

Useful options:

```powershell
# Reuse an already running app with WebView2 remote debugging on port 9333.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Measure-LovelyGitPerformanceGuardrails.ps1 -SkipLaunch

# Keep the app open after the run for inspection.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Measure-LovelyGitPerformanceGuardrails.ps1 -KeepAppRunning

# Run through dotnet run instead of the compiled debug executable.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Measure-LovelyGitPerformanceGuardrails.ps1 -UseDotNetRun
```

Artifacts are written under `artifacts/performance/`:

- `summary.json`: threshold checks, native command samples, command misses, and memory values.
- `final.png`: final app screenshot.
- `journey-trace.json`: CMG trace for the scripted journey.
- `lovelygit-performance-guardrail.cmgscript`: the generated CMG journey.

## Baseline Thresholds

These thresholds are deliberately practical local guardrails, not CI-grade guarantees. Rebaseline them only after an intentional performance change and record the reason in the PR.

| Area | Threshold |
| --- | ---: |
| Startup to usable graph rows | 4000 ms |
| Repository open / first graph page | 1500 ms |
| Graph scroll / paging response | 500 ms |
| Commit details response | 500 ms |
| Any native command round trip | 200 ms |
| Process memory sample | 200 MB |

The script exits with code `1` if any required check is missing, any area exceeds its threshold, any native command exceeds the command round-trip target, or CMG captures page/console errors.

## Reading Results

Use `summary.json` first. `checks` gives the high-level guardrail values, `misses` lists failed high-level checks, and `commandMisses` lists individual native messages that exceeded the command target. Memory values come from backend native message metrics, so they reflect samples taken during the measured commands rather than a continuous profiler trace.

If the startup or first graph values are noisy, rerun once with the same repository and compare the second run. If memory grows across repeated runs, save each `summary.json` under a unique artifact directory and compare the maximum `workingSetBytes` and `privateMemoryBytes` samples.
