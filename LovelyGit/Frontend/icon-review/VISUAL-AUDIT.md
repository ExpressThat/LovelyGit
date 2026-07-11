# Visual audit

The complete family was reviewed both at a large 64px inspection size and at LovelyGit's typical 20px production size.
The second pass uses paired 42px/20px catalog previews plus standalone close-ups for questionable metaphors. See `ICON-CHECKLIST.md` for all 100 verdicts.

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
- `incoming-commits` and `outgoing-commits`: replaced detached dots with an explicit connected commit rail beside the direction arrow.
- `remote-reference`: replaced the cloud dumbbell with a cloud flag.
- `remote-fork`: replaced the bead-like curve with a conventional Y fork.
- `reset-modes`: added an explicit reset return to the mode rows.
- `undo-last-action`: replaced the sync-like loop with a single backward action arrow.
- `discard-diff-hunk` and `stage-diff-hunk`: separated the diff rows from bounded minus/plus actions.
- `unified-diff`, `file-diff`, and `wrap-long-lines`: clarified their internal row structure at 20px.
- `conflicted-file`, `added-file`, and `deleted-file`: moved status marks into a shared inner safe area so they do not collide with the document fold.
- `confirm`, `create-new`, `remove-line`, and `close`: replaced undersized badge geometry with properly scaled standalone marks.
- `remote-actions`: corrected a radio arc that crossed the view-box boundary and recentered the complete tower.
- `incoming-commits`, `outgoing-commits`, `open-selected-result`, `amend-commit-message`, and `set-upstream`: corrected small optical-center offsets.

## Family-level checks

- All drawings remain legible without color, fill, animation, or text inside the icon.
- Destructive, remote, file, branch, navigation, and layout actions use consistent visual grammars.
- Fine detail was removed where it disappeared or collided at 20px.
- CMG geometry checks confirm all 100 rendered groups remain inside the stroked 24×24 safe area with no optical-center outliers over 1.5 units.
- Every SVG has a distinct drawing and descriptive accessible title.
- These remain review-only assets and are not imported by application source.
