# Visual audit

The complete family was reviewed both at a large 64px inspection size and at LovelyGit's typical 20px production size.

## Redrawn after the audit

- `verified-signature`: replaced a tangled overlaid seal with one clean scalloped badge and check.
- `git-worktree`: replaced overlapping full-size folder and branch drawings with a branch composed inside the folder.
- `inspect-file`: gave the document and magnifier separate visual space.
- `pull-from-remote` and `create-pull-request`: rebuilt as compact Git paths with directional endpoints instead of stacked badges.
- `fetch-remote-branch`, `publish-to-remote`, and `remote-reference`: simplified the cloud silhouette and placed the semantic cue inside it.
- `file-history`, `file-diff`, and `remove-file-content`: replaced tiny generic marks with purpose-built file compositions.
- `settings`: replaced the brightness-like sun with a recognizable gear.
- `appearance-settings`: strengthened the brush silhouette so it no longer reads as a pencil.
- `move-up` and `move-down`: added list rows so the arrows communicate reordering.
- `resize-horizontally` and `resize-vertically`: added opposing directional cues around the grip.
- `git-bisect`: placed a split Git path inside the search lens.
- `apply-stash`, `submodules`, and `initialize-submodule`: clarified package direction, plurality, and initialization.
- `remote-fork`: simplified the previous crowded fork into a single connected path.

## Family-level checks

- All drawings remain legible without color, fill, animation, or text inside the icon.
- Destructive, remote, file, branch, navigation, and layout actions use consistent visual grammars.
- Fine detail was removed where it disappeared or collided at 20px.
- Every SVG has a distinct drawing and descriptive accessible title.
- These remain review-only assets and are not imported by application source.
