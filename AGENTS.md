# AGENTS.md

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
- Never use the LovelyGit working repository, one of its branches/worktrees, or another user-owned repository as a CMG or Git workflow test repository, including for a journey intended to be read-only. A UI bug, stale selection, or accidental action could mutate it. Do not add or select `C:\Projects\LovelyGit` in automated app journeys.
- Run every CMG Git workflow against a purpose-built disposable repository under `artifacts/` or the operating system's temporary directory. Seed it with isolated test commits, give the journey exclusive ownership, verify the expected Git state directly, and remove it from LovelyGit and disk after the journey. Never switch branches, create commits or branches, rebase, reset, merge, cherry-pick, stash, discard changes, or otherwise alter a real repository merely to exercise the UI.

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

## File Size
- Keep every first-party authored source, test, script, stylesheet, configuration, and Markdown file at or below 250 physical lines. Split by responsibility before a file reaches the limit; do not compress formatting or combine statements merely to reduce the count.
- Run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-FileLength.ps1` before committing. A feature is not complete while this gate fails for a first-party file it added or changed.
- Do not edit generated, vendored, or third-party-owned files solely to satisfy the limit. The gate explicitly exempts generated frontend contracts, built assets, and the existing shadcn component directory (`LovelyGit/Frontend/src/components/ui/`). New app-specific composition should live outside the shadcn directory when it would otherwise require modifying third-party-derived primitives.
- Intentionally centralized registries may be exempt when splitting them would destroy a useful single source of truth. `LovelyGit/Services/NativeMessaging/NativeMessageType.cs` is the canonical command registry and `LovelyGit/Frontend/src/lib/settings/theme/themeCatalog.ts` is the canonical theme catalog; both are explicitly exempt. Keep long registries grouped and easy to navigate.
- Keep exemptions narrow and path-based in `scripts/Test-FileLength.ps1`. Do not exempt an ordinary app directory or a first-party file because splitting it is inconvenient.

## Base UI And Lazy Overlay Safety
- LovelyGit's shadcn components use Base UI. Preserve Base UI's required component hierarchy when adding or adapting dialogs, dropdowns, context menus, popovers, tooltips, and other portals. In particular, `ContextMenu.GroupLabel`/`Menu.GroupLabel` must be nested inside the matching `ContextMenu.Group`/`Menu.Group`. Rendering a group label directly in a popup throws Base UI production error 31.
- This class of mistake can pass TypeScript and production builds because portal content is lazy-rendered. The failure appears only when the user opens the menu or overlay; in the desktop WebView it can leave the entire content area blank. A successful build is therefore not a sufficient test for a new or changed overlay.
- After adding or changing any lazy overlay, use CMG in the real LovelyGit WebView2 app to open every new trigger at least once (including right-click triggers), exercise its principal item, and run both page-error and error-console gates. For context menus attached to multiple surfaces, open each surface separately; for example, test both a sidebar branch row and a branch tag in the commit graph.
- If the app turns blank after an interaction, attach CMG before reproducing, then inspect `listPageErrors` and `listConsole --level error`. For a minified Base UI error, find the development message in the installed package with `rg "formatErrorMessage\\(<code>" LovelyGit/Frontend/node_modules/@base-ui/react`; fix the documented component hierarchy, rebuild the frontend, relaunch the native app, reproduce the interaction, and require clean diagnostics before reporting success.

## Design System
- Use semantic Tailwind/shadcn color tokens for app UI: `bg-background`, `bg-card`, `bg-popover`, `bg-sidebar`, `bg-muted`, `bg-secondary`, `bg-accent`, `text-foreground`, `text-muted-foreground`, `border-border`, `ring-ring`, and `bg-primary` for true primary emphasis. Avoid hard-coded colors in app chrome unless a preview/demo must intentionally show a fixed sample.
- Theme colors are CSS variables applied in `Frontend/src/lib/settings/theme/themeUtils.ts`. `background` is the user's base surface color; derived surfaces such as `card`, `popover`, `muted`, `secondary`, `accent`, `sidebar`, `border`, and `input` should stay related to that background so panels, dialogs, sidebars, lists, and hover states feel like the same theme.
- Do not use the user's accent color as a general surface color. In this app, `--accent` is a shadcn interaction surface used by hover, active, selected, dropdown, and menu rows. User accent choices should drive `primary`, `ring`, `sidebar-primary`, and other emphasis tokens, while `--accent` should remain a background-derived interaction surface.
- When adding new panels or dialogs, prefer `bg-popover` for floating/dialog surfaces, `bg-card` for framed sections and repeated items, `bg-background` for the main workspace, and `bg-sidebar` for side navigation. If a surface looks like it ignores a custom theme background, check whether it is using a hard-coded color, opacity over black/white, or the wrong semantic token.
- Theme and font settings must be applied before React renders in `Frontend/src/bootstrapApp.tsx` as well as live through the React hooks, so launch does not flash the wrong theme or font.
- After changing theme behavior, visually test at least one light and one dark custom background/accent combination in the real WebView2 app with CMG, and inspect screenshots for dialogs, sidebars, top nav, commit graph rows, details/working-changes panels, dropdowns, and hover/selected states.

### Motion And Animation

- Treat tasteful motion as part of LovelyGit's visual identity. Actively look for places where animation, micro-interactions, stagger, spring movement, or a polished entrance/exit can make a user-facing feature feel more beautiful, responsive, and crafted, even when the motion is primarily aesthetic. It should still feel intentional and restrained: reinforce the interface rather than distract from the work or compete with frequently repeated actions.
- Use the existing `motion` package through `motion/react` for coordinated enter/exit animation, shared layout movement, spring interactions, and sequences that must remain mounted long enough to animate out. Prefer the existing `AnimatePresence`, `LayoutGroup`, `layout`, `motion.*`, `whileHover`, and `whileTap` patterns over introducing another animation library or hand-rolled timers.
- Treat the repository tabs as the reference for spatial layout motion: `Tabs.tsx` and `RepositoryTab.tsx` use `LayoutGroup`, `AnimatePresence`, layout springs, and restrained hover/tap responses so tabs insert, close, resize, and reorder without visually jumping.
- Treat the details and diff surfaces as the reference for navigation motion: `SlidingDetailsPanel.tsx` animates the commit-details/working-changes panel in and out while preserving workspace context, while `App.tsx` uses `AnimatePresence` with short `x`, `opacity`, and `scale` transitions when a commit diff or working-tree diff slides over its parent view. New drill-in and back-out flows should preserve that same directional relationship.
- When designing a new user-facing surface, explicitly consider its motion treatment alongside its layout, color, and typography instead of adding animation only as an afterthought. Small hover/tap responses, softly staged content, animated selection indicators, and spatially consistent transitions are encouraged when they add delight or premium visual polish.
- Keep interactions immediate: never delay the underlying action to wait for an animation. Prefer short transitions and compositor-friendly `transform` and `opacity`; use layout animation only for small bounded UI such as tabs or side panels, never for every row in the commit graph or another large virtualized list. Honor reduced-motion preferences with Motion's reduced-motion facilities or Tailwind `motion-reduce` variants, and verify important transitions in the real WebView2 app with CMG.

## Allocation And Memory Efficiency
- Prefer non-allocating implementations wherever they remain clear, safe, and measurably useful, and treat avoidable allocation as a performance bug in measured hot paths. Prefer `Span<T>`/`ReadOnlySpan<T>` for synchronous parsing and slicing, and `Memory<T>`/`ReadOnlyMemory<T>` when data must cross an asynchronous or heap-stored boundary, especially in the native Git parser, object/pack readers, diffing, serialization, and large collection transforms.
- Slice existing buffers instead of creating temporary strings or arrays. Prefer span-based parsing APIs, caller-provided buffers, streaming, pooling such as `ArrayPool<T>` for proven high-volume scratch storage, and bounded reusable caches where ownership and lifetime remain explicit.
- Do not introduce `Span<T>` or pooling mechanically. `Span<T>` is a stack-only `ref struct` and cannot safely cross `await`, iterator, capture, or long-lived object boundaries; use `ReadOnlyMemory<T>` or an owned buffer there. Do not trade clear ownership or correctness for microscopic savings in cold code.
- Back allocation-oriented changes with evidence proportional to the risk: focused benchmarks, allocation counters, representative parser/diff fixtures, or before/after runtime measurements. Preserve the simplest non-allocating API that materially improves throughput or memory use.

## Automated Testing
- Every new or changed behavior must include automated regression coverage in the same feature. Backend behavior belongs in `LovelyGit.Tests`; frontend logic and interactions belong in colocated Vitest `*.test.ts`/`*.test.tsx` files. A bug fix is incomplete until a test fails without the fix and passes with it.
- Happy-path-only coverage is not acceptable for a behavior that can reject input, fail, be cancelled, time out, partially complete, or perform a destructive action. Before considering a feature tested, enumerate its materially distinct outcomes and cover the successful path plus every relevant validation boundary, dependency/CLI failure, cancellation or timeout, destructive confirmation/cancellation, and no-mutation-on-failure invariant. If a category genuinely cannot occur, it does not need a synthetic test.
- Test the observable contract and safety outcome, not merely that a method or mock was called. Failure-path tests must assert the surfaced error and the resulting repository/UI state; destructive-command tests must prove cancellation leaves state unchanged; retryable operations must prove controls re-enable and a later retry can succeed. Prefer representative boundary cases over repetitive permutations that add no new confidence.
- Apply these standards when touching existing suites as well as new code. If an existing feature has only success coverage, add its missing high-risk non-happy paths before extending that feature; do not preserve weak tests as precedent.
- Never use the `C:\Projects\LovelyGit` working repository as a Git test fixture, even for a test that appears read-only or harmless; a failed assertion, malformed command, or future test change could damage active work. Create a fresh disposable repository under the test framework's temporary directory or `artifacts/`, give each test exclusive ownership, and constrain every mutating Git process and CMG journey to that repository. Apply the same rule to all repositories added by the user.
- Prefer fast deterministic unit tests for validators, parsers, state transitions, command construction, and presentation logic. Use temporary on-disk repositories for Git CLI/native-parser integration where real Git semantics are the behavior under test. Keep external networks, user credentials, clocks, and machine-global state out of the unit suite.
- The official backend gate is `dotnet test LovelyGit.slnx`. `LovelyGit.Tests/LovelyGit.Tests.csproj` must remain included in the solution and runnable without a private ad-hoc project. Do not leave test sources that fail to compile against the current production architecture.
- Collect backend coverage with `dotnet test LovelyGit.slnx --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory artifacts/coverage/backend`. Generated/compiler-generated code is excluded; do not exclude ordinary first-party code to improve the number.
- The official frontend gate is `pnpm test` from `LovelyGit/Frontend`; collect coverage with `pnpm test:coverage`. Use Testing Library with the jsdom environment for user-facing interactions and keep pure data/algorithm tests in the default Node environment.
- Do not hand-test a behavior in place of unit coverage when it can be automated. CMG real-app testing remains required for WebView2 integration, lazy overlays, motion, accessibility, and full user journeys, but it complements rather than replaces C# and frontend unit tests.
- Do not delete, weaken, skip, or broadly mock a failing test merely to make the suite green. First determine whether it exposes a production regression or a stale contract; fix production when the behavior is valid, and rewrite the test only when the desired current behavior is explicitly preserved by the replacement assertion.
- Coverage is a gap detector, not permission to write assertion-free tests. Cover success, validation, cancellation/error, and destructive-confirmation paths where they materially differ. Keep coverage reports under `artifacts/coverage/` and inspect uncovered first-party hot paths when planning the next tests.
- Coverage thresholds are a ratchet, not a target. Never lower or remove the configured frontend or backend coverage gates to accommodate new code; add meaningful tests and raise the thresholds as coverage improves. A passing global percentage does not excuse leaving newly changed behavior uncovered.

## Release/Runtime Gotchas
- Release CI requires Node 24, .NET 10.x and 8.x, restored .NET tools, frozen pnpm install, frontend `pnpm run prod`, then runtime-specific `dotnet publish` and Velopack packaging.
- App data is stored in `%LOCALAPPDATA%/LovelyGit/LovelyGit.blite`; the cache DB `%LOCALAPPDATA%/LovelyGit/LovelyGit.Cache.blite` is deleted at startup.
