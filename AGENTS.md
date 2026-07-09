# AGENTS.md

## Team Workflow
- Use merge commits only for branch integration and PR merge guidance in this LovelyGit workspace; do not squash merges.
- Code-changing work should produce a commit and PR. QA tests/artifacts and documentation updates should also be committed and PR'd when they are relevant to the work.
- Keep commit authorship and GitHub PR review identity separate. Local Git config controls commits; GitHub CLI/browser authentication controls PR comments, reviews, and merges.
- Before committing, verify the repo-local identity with `git config --show-origin --get user.name` and `git config --show-origin --get user.email`. If it needs to change for this workspace, use `git config --local user.name "<name>"` and `git config --local user.email "<email>"`; do not change global Git identity for a task-specific split.
- Before posting PR reviews or comments, verify the GitHub account with `gh auth status`. If it is not the intended reviewer account, switch explicitly with `gh auth switch` or set a task-scoped `GH_TOKEN`; do not rely on whichever account happens to be active.
- Alice reviews PRs with the `ExpressThat-bot` GitHub identity using the provided `GH_TOKEN`; include that review expectation in PR handoff notes when Alice is the intended reviewer.
- Do not amend, rebase, or rewrite existing commits solely to repair author identity unless the user explicitly asks for history rewriting.

## Product Direction
- Keep LovelyGit moving toward a beautiful, extremely fast Git GUI client with feature depth comparable to major Git clients.
- Preserve the existing backend/frontend communication patterns unless a task explicitly calls for a contract change.
- Pay attention to performance metrics such as interaction latency, git operation duration, and RAM use when adding or changing behavior.
- Treat QA, reviews, and docs as part of the delivery work, not optional follow-up.

## Visual Testing
- Use CMG from `C:\CMG\CMG.exe` for visual checks of the real LovelyGit desktop app, not a plain browser-only `localhost` session.
- Treat the shipped release skill `C:\CMG\SKILL.md` as the CMG usage contract. Do not inspect or depend on CMG source internals for normal LovelyGit testing guidance.
- When debugging, set `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=9333` in the Debug/launch profile environment before starting LovelyGit. For this repo's `http` profile, add it beside `ASPNETCORE_ENVIRONMENT` in `LovelyGit/Properties/launchSettings.json`, or set the same variable in the IDE's debug environment UI.
- Visual-test launches should keep the native LovelyGit window visible unless the user explicitly asks for an offscreen/minimized run.
- Use a larger visible window for dense workflows such as merge conflict resolution; the helper accepts `-Width` and `-Height` and defaults visual-test launches to a larger size than normal app startup.
- Prefer the helper script over direct foreground `dotnet run`; it sets WebView2 remote debugging and rebuilds the debug app as `WinExe` while leaving the app window visible:
  `powershell -NoProfile -ExecutionPolicy Bypass -File C:/Projects/LovelyGit/scripts/Start-LovelyGitVisualTest.ps1 -UseDotNetRun`
- Launch the compiled WebView2 app with remote debugging enabled through the same helper:
  `powershell -NoProfile -ExecutionPolicy Bypass -File C:/Projects/LovelyGit/scripts/Start-LovelyGitVisualTest.ps1`
- Confirm the WebView2 target is available with `Invoke-WebRequest -UseBasicParsing http://127.0.0.1:9333/json`; the target title should be `LovelyGit` and the URL should be `http://localhost:5000/`.
- Attach CMG to the running WebView2 app before driving it so CMG installs page diagnostics automatically:
  `C:/CMG/CMG.exe browser app attach --port 9333`
- Drive the attached app with CMG using the selected app target when available, for example `C:/CMG/CMG.exe browser control tabs list`, `C:/CMG/CMG.exe browser control script --file artifacts/lovelygit-app-graph.cmgscript --gif artifacts/lovelygit-app-graph.gif`, or `C:/CMG/CMG.exe browser control script --inline "screenshotPage output=\"artifacts/app.png\""`. If the selected app target is not honored by the current shell session, use the explicit debug-port form for every command, for example `C:/CMG/CMG.exe browser --port 9333 control tabs list`.
- Prefer `browser control script --file <path>` for multi-step user journeys and `cmg run <path> --report-json artifacts/<name>.json --trace artifacts/<name>-trace` for repeatable flow tests that need structured reports, retries, traces, or per-test GIFs.
- Use CMG rich/provider locators such as `getByRole=button|Save`, `getByLabel=Repository path`, `getByTitle=...`, and `getByText=...` where possible so visual checks also pressure accessible names. Quote locators that contain spaces.
- User-like CMG actions such as `click`, `type`, `clear`, `hover`, `select`, and `dragAndDrop` do not scroll automatically; add `scrollIntoView`, `scrollTo`, `scrollBy`, or `wheel` before interacting with off-viewport content.
- CMG now arms console and page-error diagnostics automatically when `browser launch`, `browser app launch`, or `browser app attach` succeeds. Do not require `captureConsole` or `capturePageErrors` for new workflows; they are deprecated compatibility aliases that only ensure capture is installed and do not clear existing captured entries.
- After risky UI actions, inspect captured diagnostics before gating:
  `C:/CMG/CMG.exe browser control events pageErrors listPageErrors`
  `C:/CMG/CMG.exe browser control events console listConsole --level error`
- When using the explicit debug-port form, keep that form for diagnostics too:
  `C:/CMG/CMG.exe browser --port 9333 control events pageErrors listPageErrors`
  `C:/CMG/CMG.exe browser --port 9333 control events console listConsole --level error`
- To diagnose a crash after an interaction, positive waits can still print one matching diagnostic:
  `C:/CMG/CMG.exe browser control events pageErrors wait "." --match regex --timeout 1000`
  `C:/CMG/CMG.exe browser control events console wait "." --level error --match regex --timeout 1000`
- For normal visual-test gates, after the interaction and screenshot, assert no captured frontend failures:
  `C:/CMG/CMG.exe browser control events pageErrors expectNoPageError --timeout 250`
  `C:/CMG/CMG.exe browser control events console expectNoConsole --level error --timeout 250`
- CMG diagnostics are forward-only from launch/attach/arming time; events that happened before CMG attached cannot be recovered. If a crash happened before attach, reproduce it after `browser app attach`.
- Use `accessibilitySnapshot` or `expectAccessible` when validating dialogs, context menus, custom controls, or regressions in keyboard/label behavior, and save the JSON under `artifacts/` when it is useful evidence.
- Prefer assertions against visible app UI such as `COMMIT MESSAGE`, `HASH`, `AUTHOR`, repository tabs, details panels, and working-tree controls; the new-tab `Open Repo` button only appears when no current repository is selected.
- Save screenshots/GIFs under `artifacts/` and inspect screenshots before reporting visual success. If the app shows `Missing file: index.html`, it was launched from the wrong working directory.

## Project Shape
- Single .NET solution: `LovelyGit.slnx` contains `LovelyGit/LovelyGit.csproj`, a `net10.0` Web SDK app with warnings treated as errors and AOT publishing enabled.
- Frontend lives under `LovelyGit/Frontend` and is a Vite + React 19 + TypeScript app; production frontend assets are copied into `LovelyGit/wwwroot` by the frontend build.
- App startup is `LovelyGit/Program.cs`; it hosts InfiniFrame, serves `/` from `wwwroot/index.html`, maps SignalR at `/commsHub`, and clears the git repo cache on startup/shutdown.

## Where Code Lives
- `LovelyGit/Program.cs` wires the desktop web host, window settings, static assets, SignalR hub, startup update check, and cache cleanup.
- `LovelyGit/Services/Hubs` is the backend command layer: `CommsHub` receives frontend commands, `CommandResolver` dispatches them to feature `CommandResponder<TArguments>` implementations.
- `LovelyGit/Services/Hubs/CommandResolvers/*` contains feature command handlers for known repositories, commit graph/details, and settings.
- `LovelyGit/Services/Git` contains git parsing and commit graph construction; `LovelyFastGitParser` reads repository objects and `CommitGraph` builds paged graph responses.
- `LovelyGit/Services/Data` contains BLite persistence for known repositories, settings, and the transient commit graph cache.
- `LovelyGit/Frontend/src/main.tsx` bootstraps SignalR before rendering `App`; `src/App.tsx` switches between the new-tab view and the commit graph/details UI.
- `LovelyGit/Frontend/src/components` holds React UI by feature, `src/lib` holds shared SignalR, settings, repository context, and utility code, and `src/generated` is codegen output.

## What The App Does
- LovelyGit is a desktop Git client shell: InfiniFrame hosts an ASP.NET Core backend and a React frontend inside the app window.
- The frontend talks to the backend through typed SignalR commands at `/commsHub`; responses are correlated with `commandUniqueId` in `registerSignalR.ts`.
- Users add/select known git repositories, then the backend opens the repository path, pages commit graph data, caches traversal state in `LovelyGit.Cache.blite`, and returns rows for the virtualized commit graph UI.
- Selecting a commit sends a commit-details command and displays changed-file/detail data in the sliding details panel.
- Settings are persisted in `LovelyGit.blite`; generated frontend setting types mirror backend `SettingDefinition<T>` declarations.

## Commands
- Restore .NET local tools from `LovelyGit`: `dotnet tool restore`.
- Install frontend deps from `LovelyGit/Frontend`: `pnpm install --frozen-lockfile` (repo pins `pnpm@11.3.0`).
- Regenerate frontend C# contracts from `LovelyGit/Frontend`: `pnpm generate:csharp-types`.
- Build frontend for shipping from `LovelyGit/Frontend`: `pnpm prod` (runs codegen, `tsc -b`, `vite build`, then copies `dist` to `../wwwroot`).
- Format/lint frontend from `LovelyGit/Frontend`: `pnpm lint` or `pnpm lint:fix` (both run Biome with writes).
- Build backend from repo root: `dotnet build LovelyGit/LovelyGit.csproj`.
- Publish like CI: build frontend first, then `dotnet publish LovelyGit/LovelyGit.csproj --configuration Release --framework net10.0 --runtime <rid> --self-contained true`.

## Codegen And Contracts
- Do not hand-edit `LovelyGit/Frontend/src/generated/**`; Biome intentionally excludes it.
- C# types exported to TypeScript need Tapper/source-generation wiring: use `[TranspilationSource]` where appropriate and include serializable response/argument types in the relevant `*JsonSerializerContext`.
- New SignalR commands need all pieces kept in sync: `CommsHubCommandType`, a `CommandResponder<TArguments>`, DI registration in the feature `*ServiceCollectionExtensions`, JSON source-generation entries, then rerun `pnpm generate:csharp-types`.
- Settings frontend typing is generated by `Frontend/scripts/generate-command-contracts.ts` by scanning `public static readonly SettingDefinition<T>` declarations under `LovelyGit/Services`.

## Frontend Notes
- Use the `@/*` alias for `Frontend/src/*`; it is configured in Vite and TS configs.
- React Compiler is enabled through `@vitejs/plugin-react` plus `@rolldown/plugin-babel` in `vite.config.ts`; avoid unnecessary manual memoization unless measurements or existing patterns justify it.
- UI uses Tailwind v4 plus shadcn `base-nova` config in `components.json`; local VS Code settings suppress Tailwind canonical-class suggestions.

## Design System
- Use semantic Tailwind/shadcn color tokens for app UI: `bg-background`, `bg-card`, `bg-popover`, `bg-sidebar`, `bg-muted`, `bg-secondary`, `bg-accent`, `text-foreground`, `text-muted-foreground`, `border-border`, `ring-ring`, and `bg-primary` for true primary emphasis. Avoid hard-coded colors in app chrome unless a preview/demo must intentionally show a fixed sample.
- Theme colors are CSS variables applied in `Frontend/src/lib/settings/theme/themeUtils.ts`. `background` is the user's base surface color; derived surfaces such as `card`, `popover`, `muted`, `secondary`, `accent`, `sidebar`, `border`, and `input` should stay related to that background so panels, dialogs, sidebars, lists, and hover states feel like the same theme.
- Do not use the user's accent color as a general surface color. In this app, `--accent` is a shadcn interaction surface used by hover, active, selected, dropdown, and menu rows. User accent choices should drive `primary`, `ring`, `sidebar-primary`, and other emphasis tokens, while `--accent` should remain a background-derived interaction surface.
- When adding new panels or dialogs, prefer `bg-popover` for floating/dialog surfaces, `bg-card` for framed sections and repeated items, `bg-background` for the main workspace, and `bg-sidebar` for side navigation. If a surface looks like it ignores a custom theme background, check whether it is using a hard-coded color, opacity over black/white, or the wrong semantic token.
- Theme and font settings must be applied before React renders in `Frontend/src/bootstrapApp.tsx` as well as live through the React hooks, so launch does not flash the wrong theme or font.
- After changing theme behavior, visually test at least one light and one dark custom background/accent combination in the real WebView2 app with CMG, and inspect screenshots for dialogs, sidebars, top nav, commit graph rows, details/working-changes panels, dropdowns, and hover/selected states.

## Testing State
- No test projects, frontend test runner, or CI verification workflow are present. For focused verification, run the relevant build/codegen command above and mention any unverified runtime behavior.

## Release/Runtime Gotchas
- Release CI requires Node 24, .NET 10.x and 8.x, restored .NET tools, frozen pnpm install, frontend `pnpm run prod`, then runtime-specific `dotnet publish` and Velopack packaging.
- App data is stored in `%LOCALAPPDATA%/LovelyGit/LovelyGit.blite`; the cache DB `%LOCALAPPDATA%/LovelyGit/LovelyGit.Cache.blite` is deleted at startup.
