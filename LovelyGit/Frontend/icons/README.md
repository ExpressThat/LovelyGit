# LovelyGit icons

This folder contains LovelyGit's production icon source assets: unmodified SVGs from [Tabler Icons](https://tabler.io/icons). The frontend consumes the generated sprite at `src/assets/lovely-icons.svg` through the shared icon components.

- Open `index.html` to compare the complete family at 42px and 20px.
- Individual assets live in `svg/` under LovelyGit-specific filenames.
- `SOURCE-MAP.md` links every local filename to its exact upstream SVG.
- `TABLER-LICENSE.txt` contains the required MIT license notice.
- The upstream revision is pinned in `scripts/generate-icons.mjs`; the script copies the SVGs without modification and rebuilds the runtime sprite.

Regenerate from the repository root with network access, or with the matching Tabler checkout under `artifacts/tabler-icons-upstream`:

```powershell
node scripts/generate-icons.mjs
```
