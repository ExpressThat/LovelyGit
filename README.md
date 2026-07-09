# LovelyGit

LovelyGit is a desktop Git client that hosts an ASP.NET Core backend and a
React frontend inside a native WebView2 window. It focuses on fast repository
navigation, commit graph exploration, commit details, working-tree changes, and
customizable app appearance.

## Documentation

- [LovelyGit workflow documentation gaps](docs/lovelygit-workflow-docs-gaps.md)
  records the current user-facing documentation gaps and proposed updates for
  repository selection, commit graph navigation, settings/theme behavior, and
  CMG visual testing.
- [Agent workflow instructions](AGENTS.md) describe repository structure,
  commands, design-system rules, and CMG visual testing expectations for
  contributors working in this workspace.

## Development

Restore tools and build from the repository root:

```powershell
dotnet tool restore --tool-manifest LovelyGit/dotnet-tools.json
dotnet build LovelyGit/LovelyGit.csproj
```

Install frontend dependencies and run frontend checks from `LovelyGit/Frontend`:

```powershell
pnpm install --frozen-lockfile
pnpm generate:csharp-types
pnpm lint
pnpm test
```

For a shipping-style frontend build, run:

```powershell
pnpm prod
```

For real LovelyGit visual checks, use CMG against the WebView2 app as described
in `AGENTS.md`; browser-only `localhost` checks are not a substitute for native
desktop visual testing.
