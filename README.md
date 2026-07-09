# LovelyGit

LovelyGit is a desktop Git client that pairs an ASP.NET Core host with a React
frontend inside a WebView2 window. It is focused on repository navigation,
commit graph exploration, commit/file review, working-tree changes, and common
repository actions from a native desktop shell.

## Current product workflows

The main app flow is documented in [docs/workflows.md](docs/workflows.md). It
covers:

- Opening and switching known repositories.
- Browsing the commit graph, refs, worktrees, stashes, and commit details.
- Reviewing commit file diffs and patch text.
- Reviewing, staging, unstaging, discarding, and committing working-tree changes.
- Running fetch, pull, push, checkout, branch create/delete/rename, reveal, and
  terminal actions from the app.
- Using settings for appearance, graph, file display, and remote-action behavior.

## Development quick start

Prerequisites for normal development are .NET 10, Node 24, and pnpm 11.3.0.

Install frontend dependencies:

```powershell
pnpm --dir LovelyGit/Frontend install --frozen-lockfile
```

Build the production frontend assets served by the desktop host:

```powershell
pnpm --dir LovelyGit/Frontend prod
```

Build the .NET app:

```powershell
dotnet build LovelyGit/LovelyGit.csproj
```

For frontend-only command details, generated contracts, and source layout, see
[LovelyGit/Frontend/README.md](LovelyGit/Frontend/README.md).

## QA and evidence

LovelyGit visual checks should run against the real WebView2 desktop app, not
only a browser-hosted Vite session. Reusable CMG flows and evidence conventions
are documented in [scripts/cmg/lovelygit-qa/README.md](scripts/cmg/lovelygit-qa/README.md).

Performance gates and current known blockers are tracked in
[docs/performance-baseline.md](docs/performance-baseline.md).

Product roadmap priorities, feature inventory, lane ownership, and definition of
done guidance are tracked in
[docs/product-roadmap.md](docs/product-roadmap.md).

## Contributor notes

- Do not edit generated files under `LovelyGit/Frontend/src/generated` by hand.
  Regenerate frontend contracts from `LovelyGit/Frontend` with
  `pnpm generate:csharp-types` when backend contracts change.
- Keep user-facing workflow docs current when adding, removing, or renaming app
  commands, panels, settings, or QA flows.
- Store one-off screenshots, GIFs, traces, and exploratory CMG scripts under
  `artifacts/`; reusable scripts belong under `scripts/cmg/`.
