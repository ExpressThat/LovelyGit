# LovelyGit CMG scripts

Repository-owned CMG scripts live here when they are reusable across agents or review cycles. Keep one-off debugging scripts and generated evidence under `artifacts/`, which is ignored by git.

## Script sets

- `lovelygit-qa/`: repeatable visual, accessibility, and diagnostics flows for the real LovelyGit WebView2 desktop app.

Run LovelyGit through `scripts/Start-LovelyGitVisualTest.ps1`, attach CMG to port `9333`, then run the relevant `.cmgscript` file. See each script set README for preconditions and example commands.
