import { mkdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";

const revision = "6d128ed935d4546607b1e4d5d08c8b27bdbe7758";
const repository = "https://github.com/tabler/tabler-icons";
const output = path.resolve("LovelyGit/Frontend/icon-review");
const checkout = path.resolve("artifacts/tabler-icons-upstream");
const icons = [
	["added-file", "file-plus"], ["amend-commit-message", "pencil"], ["appearance-settings", "brush"],
	["apply-patch-file", "file-import"], ["apply-stash", "package-import"], ["auto-configure", "wand"],
	["bisect-bad", "circle-x"], ["bisect-good", "circle-check"], ["close", "x"],
	["collapse-left", "chevron-left"], ["collapse-unchanged-lines", "arrows-minimize"], ["command-palette", "command"],
	["commit-information", "info-circle"], ["compare-revisions", "arrows-diff"], ["confirm", "check"],
	["conflicted-file", "file-alert"], ["continue-operation", "player-play"], ["copy-to-clipboard", "copy"],
	["create-new", "plus"], ["create-pull-request", "git-pull-request"], ["dangerous-operation", "shield-exclamation"],
	["delete-permanently", "trash"], ["deleted-file", "file-x"], ["discard-diff-hunk", "file-minus"],
	["disconnect-submodule", "plug-x"], ["download-from-remote", "download"], ["expand-down", "chevron-down"],
	["expand-right", "chevron-right"], ["expand-up", "chevron-up"], ["fetch-remote-branch", "cloud-download"],
	["file-diff", "file-diff"], ["file-history", "file-time"], ["file", "file"],
	["full-file-view", "file-text"], ["git-bisect", "git-compare"], ["git-branch", "git-branch"],
	["git-commit", "git-commit"], ["git-identity", "user"], ["git-lfs", "database-import"],
	["git-tag", "tag"], ["git-worktree", "folder-code"], ["hide-unmodified-lines", "eye-off"],
	["incoming-commits", "arrow-big-down-lines"], ["initialize-submodule", "subtask"], ["inspect-file", "file-search"],
	["interactive-rebase", "list-tree"], ["lfs-tracked-file", "file-database"], ["local-worktree", "folder-root"],
	["lock-worktree", "lock"], ["merge-branches", "git-merge"], ["move-down", "sort-descending"],
	["move-up", "sort-ascending"], ["omit-change", "ban"], ["open-external-link", "external-link"],
	["open-merge-tool", "tools"], ["open-repository", "folder-open"], ["open-selected-result", "corner-down-left"],
	["open-terminal", "terminal-2"], ["operation-in-progress", "loader-2"], ["outgoing-commits", "arrow-big-up-lines"],
	["publish-to-remote", "cloud-upload"], ["pull-from-remote", "arrow-down-from-arc"], ["push-to-remote", "arrow-up-to-arc"],
	["reflog-entry", "clipboard-list"], ["reflog-history", "history"], ["refresh-repository", "refresh"],
	["remote-actions", "antenna"], ["remote-fork", "git-fork"], ["remote-reference", "cloud-code"],
	["remove-file-content", "file-off"], ["remove-line", "minus"], ["remove-upstream", "unlink"],
	["rename-or-edit", "edit"], ["reset-modes", "adjustments-cog"], ["reset-revision", "history-toggle"],
	["resize-horizontally", "arrows-horizontal"], ["resize-vertically", "arrows-vertical"], ["restore-default", "restore"],
	["restore-stash", "package-export"], ["save-changes", "device-floppy"], ["search", "search"],
	["set-upstream", "link"], ["settings", "settings"], ["show-whitespace", "pilcrow"],
	["side-by-side-diff", "columns-2"], ["skip-bisect-commit", "player-skip-forward"], ["stage-changes", "square-check"],
	["stage-diff-hunk", "text-plus"], ["stash", "archive"], ["submodule", "box"],
	["submodules", "packages"], ["toggle-refs-panel", "layout-sidebar-left-expand"], ["undo-last-action", "arrow-back-up"],
	["unified-diff", "layout-rows"], ["unknown-file", "file-unknown"], ["unlock-worktree", "lock-open"],
	["unstage-changes", "square-minus"], ["verified-signature", "rosette-discount-check"], ["warning-triangle", "alert-triangle"],
	["wrap-long-lines", "text-wrap"],
];

async function getUpstream(relativePath) {
	try {
		return await readFile(path.join(checkout, relativePath), "utf8");
	} catch {
		const response = await fetch(`https://raw.githubusercontent.com/tabler/tabler-icons/${revision}/${relativePath}`);
		if (!response.ok) throw new Error(`Unable to fetch ${relativePath}: ${response.status}`);
		return response.text();
	}
}

await mkdir(path.join(output, "svg"), { recursive: true });
await Promise.all(icons.map(async ([localName, upstreamName]) => {
	const source = await getUpstream(`icons/outline/${upstreamName}.svg`);
	await writeFile(path.join(output, "svg", `${localName}.svg`), source);
}));
await writeFile(path.join(output, "TABLER-LICENSE.txt"), await getUpstream("LICENSE"));

const sourceRows = icons.map(([localName, upstreamName]) =>
	`| \`${localName}.svg\` | [\`${upstreamName}\`](${repository}/blob/${revision}/icons/outline/${upstreamName}.svg) |`).join("\n");
const sourceMap = `# Icon sources\n\nAll assets are unmodified Tabler Icons outline SVGs from revision \`${revision}\`, licensed under MIT.\n\n| Local asset | Upstream source |\n| --- | --- |\n${sourceRows}\n`;
await writeFile(path.join(output, "SOURCE-MAP.md"), sourceMap);

const cards = icons.map(([localName, upstreamName]) =>
	`<a href="svg/${localName}.svg" target="_blank" rel="noopener" aria-label="Open ${localName.replaceAll("-", " ")} SVG"><figure><div class="previews"><span><img class="large" src="svg/${localName}.svg" alt=""><small>42</small></span><span><img class="compact" src="svg/${localName}.svg" alt=""><small>20</small></span></div><figcaption>${localName}<small>Tabler: ${upstreamName}</small></figcaption></figure></a>`).join("");
const html = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>LovelyGit icon review</title><style>:root{color-scheme:dark}body{margin:0;background:#0e0a12;color:#eee6f5;font:14px system-ui;padding:32px}h1{font-size:24px}p{color:#b7aabd}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(170px,1fr));gap:12px}.grid>a{color:inherit;text-decoration:none;border-radius:12px}.grid>a:focus-visible{outline:2px solid #91f7dc;outline-offset:3px}figure{margin:0;border:1px solid #362c3e;border-radius:12px;background:#17111d;padding:14px;transition:border-color 120ms ease,transform 120ms ease}.grid>a:hover figure{border-color:#74617f;transform:translateY(-1px)}.previews{display:flex;align-items:center;justify-content:center;gap:24px;height:74px;border-radius:8px;background:#211827}.previews span{display:grid;place-items:center;gap:3px}.previews img{filter:invert(95%) sepia(15%) saturate(850%) hue-rotate(95deg)}.previews .large{width:42px;height:42px}.previews .compact{width:20px;height:20px}.previews small{color:#8f8299;font-size:9px;line-height:1}figcaption{margin-top:10px;font-size:12px;overflow-wrap:anywhere}figcaption small{display:block;margin-top:4px;color:#8f8299}@media(prefers-reduced-motion:reduce){figure{transition:none}.grid>a:hover figure{transform:none}}</style></head><body><h1>LovelyGit open-source icon review</h1><p>${icons.length} unmodified Tabler Icons at 42px and 20px. Select any card to open its local SVG.</p><main class="grid">${cards}</main></body></html>`;
await writeFile(path.join(output, "index.html"), html);
