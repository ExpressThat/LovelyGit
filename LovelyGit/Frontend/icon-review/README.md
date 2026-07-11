# LovelyGit icon concepts

This folder is a review-only visual proposal. Nothing in the application imports these SVGs.

- Open `index.html` to review the complete family; every card links directly to its SVG source.
- Read `VISUAL-AUDIT.md` for the production-size findings and redraw decisions.
- Read `ICON-CHECKLIST.md` for the individual verdict on every asset.
- Individual assets live in `svg/` and use descriptive, action-oriented filenames.
- Every icon uses a 24×24 view box, `currentColor`, rounded 2px strokes, geometric-precision rendering, and no baked-in theme color.
- Git actions combine a recognizable Git structure with a directional or status cue instead of relying on a generic arrow alone.

Regenerate the catalog from the repository root with:

```powershell
node scripts/generate-icon-review.mjs
```
