# LovelyGit open-source icon review

This folder contains review-only, unmodified SVGs from [Tabler Icons](https://tabler.io/icons). Nothing in the application imports them yet.

- Open `index.html` to compare the complete family at 42px and 20px.
- Individual assets live in `svg/` under LovelyGit-specific filenames.
- `SOURCE-MAP.md` links every local filename to its exact upstream SVG.
- `TABLER-LICENSE.txt` contains the required MIT license notice.
- The upstream revision is pinned in `scripts/generate-icon-review.mjs`; the script copies the SVGs without modification.

Regenerate from the repository root with network access, or with the matching Tabler checkout under `artifacts/tabler-icons-upstream`:

```powershell
node scripts/generate-icon-review.mjs
```
