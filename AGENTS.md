# AGENTS.md

## Visual Testing
- Use CMG from `C:\CMG\CMG.exe` for visual checks of the real LovelyGit desktop app, not a plain browser-only `localhost` session.
- When debugging, set `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=9333` in the Debug/launch profile environment before starting LovelyGit. For this repo's `http` profile, add it beside `ASPNETCORE_ENVIRONMENT` in `LovelyGit/Properties/launchSettings.json`, or set the same variable in the IDE's debug environment UI.
- Visual-test launches must also set `LOVELYGIT_TEST_WINDOW_OFFSCREEN=true` so the native window is placed away from the user's main monitor while CMG drives the WebView2 target.
- Do not run visual tests with a direct foreground `dotnet run`, because its console window can steal focus and interrupt fullscreen apps. Use the helper script so the host console is suppressed, the debug app is built as `WinExe`, and the native app window is off-main-monitor:
  `powershell -NoProfile -ExecutionPolicy Bypass -File C:/Projects/LovelyGit/scripts/Start-LovelyGitVisualTest.ps1 -UseDotNetRun`
- Launch the compiled WebView2 app with remote debugging enabled through the same helper:
  `powershell -NoProfile -ExecutionPolicy Bypass -File C:/Projects/LovelyGit/scripts/Start-LovelyGitVisualTest.ps1`
- Confirm the WebView2 target is available with `Invoke-WebRequest -UseBasicParsing http://127.0.0.1:9333/json`; the target title should be `LovelyGit` and the URL should be `http://localhost:5000/`.
- Drive the attached app with CMG using the explicit port form, for example `C:/CMG/CMG.exe browser --port 9333 control tabs list` or `C:/CMG/CMG.exe browser --port 9333 control script --file artifacts/lovelygit-app-graph.cmgscript --gif artifacts/lovelygit-app-graph.gif`.
- Always arm CMG frontend error capture before navigation, clicks, typing, or other interactions that might crash React. Use the CLI event commands directly, or the equivalent CMG script commands `captureConsole` and `capturePageErrors` before the interaction:
  `C:/CMG/CMG.exe browser --port 9333 control events console capture`
  `C:/CMG/CMG.exe browser --port 9333 control events pageErrors capture`
- To diagnose a crash after an interaction, use positive waits so CMG prints the captured diagnostics:
  `C:/CMG/CMG.exe browser --port 9333 control events pageErrors wait "." --match regex --timeout 1000`
  `C:/CMG/CMG.exe browser --port 9333 control events console wait "." --level error --match regex --timeout 1000`
- For normal visual-test gates, after the interaction and screenshot, assert no captured frontend failures:
  `C:/CMG/CMG.exe browser --port 9333 control events pageErrors expectNoPageError --timeout 250`
  `C:/CMG/CMG.exe browser --port 9333 control events console expectNoConsole --level error --timeout 250`
- CMG captures future console/page errors only after capture is armed; it does not dump browser console history from before capture was armed. If a crash already happened without capture enabled, reproduce it with capture enabled instead of relying on after-the-fact console history.
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

## Testing State
- No test projects, frontend test runner, or CI verification workflow are present. For focused verification, run the relevant build/codegen command above and mention any unverified runtime behavior.

## Release/Runtime Gotchas
- Release CI requires Node 24, .NET 10.x and 8.x, restored .NET tools, frozen pnpm install, frontend `pnpm run prod`, then runtime-specific `dotnet publish` and Velopack packaging.
- App data is stored in `%LOCALAPPDATA%/LovelyGit/LovelyGit.blite`; the cache DB `%LOCALAPPDATA%/LovelyGit/LovelyGit.Cache.blite` is deleted at startup.
