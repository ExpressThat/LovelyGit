import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";

const output = path.resolve("LovelyGit/Frontend/icon-review");
const icons = [
	["alert", "warning-triangle"], ["archive", "stash"], ["archiveRestore", "restore-stash"],
	["moveDown", "move-down"], ["moveUp", "move-up"], ["verified", "verified-signature"], ["ban", "omit-change"],
	["incoming", "incoming-commits"], ["outgoing", "outgoing-commits"],
	["box", "submodule"], ["boxes", "submodules"], ["brush", "appearance-settings"], ["check", "confirm"],
	["expandDown", "expand-down"], ["left", "collapse-left"], ["right", "expand-right"], ["expandUp", "expand-up"],
	["success", "bisect-good"], ["failure", "bisect-bad"], ["reflogEntry", "reflog-entry"],
	["copy", "copy-to-clipboard"], ["remote", "remote-reference"], ["downloadCloud", "fetch-remote-branch"],
	["uploadCloud", "publish-to-remote"], ["columns", "side-by-side-diff"], ["command", "command-palette"],
	["enter", "open-selected-result"], ["download", "download-from-remote"], ["external", "open-external-link"],
	["hidden", "hide-unmodified-lines"], ["file", "file"], ["fileArchive", "lfs-tracked-file"],
	["fileClock", "file-history"], ["fileDiff", "file-diff"], ["fileInput", "apply-patch-file"],
	["fileEmpty", "remove-file-content"], ["filePlus", "added-file"], ["fileQuestion", "unknown-file"],
	["fileSearch", "inspect-file"], ["fileText", "full-file-view"], ["fileWarning", "conflicted-file"],
	["fileDelete", "deleted-file"], ["folderGit", "git-worktree"], ["folderOpen", "open-repository"],
	["branch", "git-branch"], ["commit", "git-commit"], ["compare", "compare-revisions"],
	["fork", "remote-fork"], ["merge", "merge-branches"], ["pull", "pull-from-remote"],
	["pullRequest", "create-pull-request"], ["gripH", "resize-vertically"], ["gripV", "resize-horizontally"],
	["drive", "local-worktree"], ["driveDownload", "git-lfs"], ["history", "reflog-history"],
	["info", "commit-information"], ["layers", "reset-modes"], ["link", "set-upstream"],
	["collapse", "collapse-unchanged-lines"], ["hunkMinus", "discard-diff-hunk"], ["hunkPlus", "stage-diff-hunk"],
	["reset", "reset-revision"], ["tree", "interactive-rebase"], ["loader", "operation-in-progress"],
	["lock", "lock-worktree"], ["unlock", "unlock-worktree"], ["minus", "remove-line"],
	["unstage", "unstage-changes"], ["packageOpen", "apply-stash"], ["panel", "toggle-refs-panel"],
	["pencil", "rename-or-edit"], ["pencilLine", "amend-commit-message"], ["paragraph", "show-whitespace"],
	["play", "continue-operation"], ["plus", "create-new"], ["tower", "remote-actions"],
	["refresh", "refresh-repository"], ["undo", "restore-default"], ["rows", "unified-diff"],
	["save", "save-changes"], ["search", "search"], ["searchCode", "git-bisect"],
	["settings", "settings"], ["shield", "dangerous-operation"], ["skip", "skip-bisect-commit"],
	["sparkle", "auto-configure"], ["stage", "stage-changes"], ["terminal", "open-terminal"],
	["tag", "git-tag"], ["trash", "delete-permanently"], ["undoAction", "undo-last-action"],
	["unlink", "remove-upstream"], ["unplug", "disconnect-submodule"], ["push", "push-to-remote"],
	["user", "git-identity"], ["wand", "initialize-submodule"], ["wrap", "wrap-long-lines"],
	["wrench", "open-merge-tool"], ["close", "close"],
];

const p = (d) => `<path d="${d}"/>`;
const line = (x1,y1,x2,y2) => `<path d="M${x1} ${y1}L${x2} ${y2}"/>`;
const circle = (cx,cy,r=2) => `<circle cx="${cx}" cy="${cy}" r="${r}"/>`;
const rect = (x,y,w,h,r=2) => `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${r}"/>`;
const badge = (mark) => `<path class="accent" d="${mark}"/>`;
const file = () => p("M6 3.5h7l5 5V20.5H6zM13 3.5v5h5");
const folder = () => p("M3.5 7h6l2-2h9v14h-17z");
const arrow = (direction) => direction === "up" ? p("M12 20V5m-5 6 5-6 5 6") : p("M12 4v15m-5-6 5 6 5-6");
const branch = () => `${circle(7,5)}${circle(17,8)}${circle(7,19)}${p("M7 7v10M9 17c5 0 8-2 8-7")}`;
const marks = {
	plus:"M12 8v8M8 12h8", minus:"M8 12h8", check:"m8 12 3 3 6-7", close:"m8 8 8 8m0-8-8 8",
	download:"M12 6v9m-4-4 4 4 4-4M5 19h14", upload:"M12 18V9m-4 4 4-4 4 4M5 5h14",
	search:"M10.5 5a5.5 5.5 0 1 0 0 11 5.5 5.5 0 0 0 0-11m4 9 5 5",
};

function draw(kind) {
	if (kind === "branch") return branch();
	if (kind === "merge") return `${circle(6,5)}${circle(18,5)}${circle(12,19)}${p("M6 7c0 6 6 5 6 10M18 7c0 6-6 5-6 10")}`;
	if (kind === "commit") return `${line(3,12,21,12)}${circle(12,12,4)}`;
	if (kind === "compare") return `${p("M4 8h13m-4-4 4 4-4 4M20 16H7m4-4-4 4 4 4")}`;
	if (kind === "fork") return `${p("M4 16a4 4 0 0 1 4-4h8a4 4 0 0 0 4-4")}${circle(4,18)}${circle(20,6)}${circle(12,12)}`;
	if (kind === "fileClock") return `${p("M5 3h7l4 4v4M12 3v5h4")}${circle(15,16,5)}${p("M15 13v3l2 1")}`;
	if (kind === "fileSearch") return `${p("M5 3h7l4 4v5M12 3v5h4")}${circle(14,16,4)}${line(17,19,20,22)}`;
	if (kind === "fileDiff") return `${file()}${p("M9 11h2m2 0h2M9 15h2m2 0h2M10 10v2m4 2v2")}`;
	if (kind === "fileEmpty") return `${file()}${p("M9 15h6")}${line(8,10,16,18)}`;
	if (kind.startsWith("file")) {
		const overlay = kind.includes("Plus") ? marks.plus : kind.includes("Delete") ? marks.close : kind.includes("Minus") ? marks.minus
			: kind.includes("Search") ? marks.search : kind.includes("Warning") ? "M12 10v4m0 3h.01"
			: kind.includes("Clock") ? "M11 11v4l3 2" : kind === "fileArchive" ? "M9 11h6v6H9zm0 2h6"
			: kind === "fileDiff" ? "M9 11h2m2 4h2m-6 0h2m2-4h2" : kind === "fileInput" ? "M9 14h6m-3-3 3 3-3 3"
			: kind === "fileQuestion" ? "M10 11a2 2 0 1 1 2 2v1m0 3h.01" : kind === "fileText" ? "M9 11h6m-6 3h6m-6 3h4"
			: kind === "fileEmpty" ? "M9 11h6m-6 3h6m-6 3h6" : "M9 12h6";
		return `${file()}${kind === "file" ? "" : badge(overlay)}`;
	}
	if (kind === "folderOpen") return `${folder()}${p("M9 11h8m-3-3 3 3-3 3")}`;
	if (kind === "folderGit") return `${folder()}${circle(8,11,1)}${circle(15,11,1)}${circle(11,16,1)}${p("M9 11h4a2 2 0 0 1 2 2v0a3 3 0 0 1-3 3")}`;
	if (kind === "download") return `${arrow("down")}${line(5,20,19,20)}`;
	if (kind === "moveDown" || kind === "moveUp") return `${p("M4 6h7M4 12h5M4 18h7")}${p(kind==="moveUp"?"M17 19V5m-4 4 4-4 4 4":"M17 5v14m-4-4 4 4 4-4")}`;
	if (kind === "incoming" || kind === "outgoing") return `${arrow(kind==="outgoing"?"up":"down")}${circle(5,12,1.5)}${line(3,12,7,12)}`;
	if (kind === "expandDown" || kind === "expandUp") return p(kind==="expandUp"?"m5 15 7-7 7 7":"m5 9 7 7 7-7");
	if (kind === "push") return `${arrow("up")}${line(5,4,19,4)}`;
	if (kind === "downloadCloud" || kind === "uploadCloud") return `${p("M5 18h14a3 3 0 0 0 .5-6A5 5 0 0 0 10 9a4 4 0 0 0-5 4 2.5 2.5 0 0 0 0 5")}${p(kind==="downloadCloud"?"M12 8v7m-3-3 3 3 3-3":"M12 16V9m-3 3 3-3 3 3")}`;
	if (kind === "remote") return `${p("M5 18h14a3 3 0 0 0 .5-6A5 5 0 0 0 10 9a4 4 0 0 0-5 4 2.5 2.5 0 0 0 0 5")}${circle(9,14,1)}${circle(15,14,1)}${line(10,14,14,14)}`;
	if (kind === "pull") return `${circle(5,5,1.5)}${circle(5,18,1.5)}${p("M5 7v9M11 6h7v11m-3-3 3 3 3-3")}`;
	if (kind === "pullRequest") return `${circle(5,5,1.5)}${circle(5,19,1.5)}${p("M5 7v10M11 18h3a4 4 0 0 0 4-4V6m-3 3 3-3 3 3")}`;
	if (kind === "drive" || kind === "driveDownload") return `${rect(4,6,16,12,3)}${line(7,14,17,14)}${circle(8,10,1)}${kind==="driveDownload"?badge(marks.download):""}`;
	if (kind === "tag") return `${p("M4 5h8l8 8-7 7-9-9z")}${circle(8,9,1)}`;
	if (kind === "archive" || kind === "archiveRestore") return `${rect(4,7,16,13,2)}${p("M3 4h18v4H3zM9 12h6")}${kind==="archiveRestore"?badge("M9 17H6v-3m0 3 4-4"):""}`;
	if (kind === "history") return `${circle(12,12,8)}${p("M12 7v5l3 2M5 6H2v-3")}`;
	if (kind === "search") return p(marks.search);
	if (kind === "searchCode") return `${circle(10,10,6)}${line(14,14,20,20)}${circle(7,9,1)}${circle(13,9,1)}${p("M7 10v3m6-3v3M8 13h4")}`;
	if (kind === "trash") return `${p("M6 7h12m-9 0V4h6v3m2 0-1 13H8L7 7m4 4v5m3-5v5")}`;
	if (kind === "settings") return `${circle(12,12,3)}${p("M9 4.5 10 2h4l1 2.5 2.4 1.4 2.6-.4 2 3.5-1.7 2v2.8l1.7 2-2 3.5-2.6-.4L15 19.5 14 22h-4l-1-2.5-2.4-1.4-2.6.4-2-3.5 1.7-2v-2.8L2 8l2-3.5 2.6.4z")}`;
	if (kind === "alert" || kind === "shield") return `${p(kind==="shield"?"M12 3 20 6v6c0 5-3 8-8 10-5-2-8-5-8-10V6z":"M12 3 22 20H2z")}${badge("M12 8v5m0 4h.01")}`;
	if (kind === "lock" || kind === "unlock") return `${rect(5,10,14,10,2)}${p(kind==="lock"?"M8 10V7a4 4 0 0 1 8 0v3":"M9 10V7a4 4 0 0 1 7-2")}${circle(12,15,1)}`;
	if (kind === "external") return `${rect(4,6,14,14,2)}${p("M12 4h8v8m0-8-9 9")}`;
	if (kind === "copy") return `${rect(7,6,12,14,2)}${rect(4,3,12,14,2)}`;
	if (kind === "reflogEntry") return `${rect(5,5,14,16,2)}${p("M9 3h6v4H9zM8 11h8m-8 4h5")}`;
	if (kind === "columns") return `${rect(3,4,18,16,2)}${line(12,4,12,20)}`;
	if (kind === "rows") return `${rect(3,4,18,16,2)}${line(3,12,21,12)}`;
	if (kind === "terminal") return `${rect(3,5,18,14,2)}${p("m7 9 3 3-3 3m6 0h4")}`;
	if (kind === "user") return `${circle(12,8,4)}${p("M4 21c1-5 4-7 8-7s7 2 8 7")}`;
	if (kind === "verified") return `${p("M12 3l2 2 3-.4.8 2.8 2.7 1.3-1.2 2.7 1.2 2.7-2.7 1.3-.8 2.8-3-.4-2 2-2-2-3 .4-.8-2.8-2.7-1.3 1.2-2.7-1.2-2.7 2.7-1.3.8-2.8 3 .4z")}${p(marks.check)}`;
	if (kind === "success") return `${circle(12,12,8)}${p(marks.check)}`;
	if (kind === "failure") return `${circle(12,12,8)}${p(marks.close)}`;
	if (kind === "ban") return `${circle(12,12,8)}${line(6,18,18,6)}`;
	if (kind === "box") return `${p("M4 8l8-4 8 4-8 4zM4 8v8l8 4 8-4V8M12 12v8")}`;
	if (kind === "boxes") return `${rect(3,4,7,7,1)}${rect(14,4,7,7,1)}${rect(8.5,14,7,7,1)}${p("M6.5 11v1.5h5.5V14m5.5-3v1.5H12")}`;
	if (kind === "packageOpen") return `${p("M4 9l8 4 8-4M4 9l4-5 8 0 4 5v7l-8 4-8-4zM12 13v7")}${p("M12 4v6m-3-3 3 3 3-3")}`;
	if (kind === "brush") return `${p("m14 4 6 6-8 8H6v-6zM12 6l6 6M6 18c-3 0-4 2-3 3 2 1 5 0 5-2")}`;
	if (kind === "command") return `${p("M8 8V6a3 3 0 1 0-3 3h14a3 3 0 1 0-3-3v12a3 3 0 1 0 3-3H5a3 3 0 1 0 3 3z")}`;
	if (kind === "enter") return `${p("M19 5v7H7m4-4-4 4 4 4")}`;
	if (kind === "hidden") return `${p("M3 12c3-5 6-7 9-7s6 2 9 7c-3 5-6 7-9 7s-6-2-9-7M4 4l16 16")}`;
	if (kind === "info") return `${circle(12,12,9)}${p("M12 11v6m0-10h.01")}`;
	if (kind === "layers") return `${p("m4 9 8-5 8 5-8 5zM4 13l8 5 8-5M6 17l6 4 6-4")}`;
	if (kind === "collapse") return `${p("M4 6h16M4 18h16M8 10l4 4 4-4")}`;
	if (kind === "hunkMinus" || kind === "hunkPlus") return `${p("M5 5h9M5 9h14M5 13h8M5 17h14")}${badge(kind==="hunkPlus"?marks.plus:marks.minus)}`;
	if (kind === "reset" || kind === "undo" || kind === "undoAction") return `${p("M8 7H4v-4m0 4 4-4M5 8a8 8 0 1 1-1 7")}${kind==="reset"?badge("M9 12h6M9 15h4"):kind==="undoAction"?badge("M16 16h4v4"):""}`;
	if (kind === "tree") return `${circle(6,5,1.5)}${circle(6,12,1.5)}${circle(18,8,1.5)}${circle(18,18,1.5)}${p("M7.5 5H12v13h4.5M7.5 12H12m0-4h4.5")}`;
	if (kind === "loader") return `${p("M12 3v3m6.4-.4-2.1 2.1M21 12h-3m.4 6.4-2.1-2.1M12 21v-3m-6.4.4 2.1-2.1M3 12h3m-.4-6.4 2.1 2.1")}`;
	if (kind === "unstage" || kind === "stage") return `${rect(4,4,16,16,3)}${p(kind==="stage"?marks.check:marks.minus)}`;
	if (kind === "panel") return `${rect(3,4,18,16,2)}${line(9,4,9,20)}${p("m6 10-2 2 2 2")}`;
	if (kind === "pencilLine") return `${p("m5 16 9-9 3 3-9 9H5zM4 21h16")}`;
	if (kind === "paragraph") return `${p("M18 5h-7a4 4 0 0 0 0 8h3m0-8v14m4-14v14")}`;
	if (kind === "tower") return `${line(12,19,12,11)}${circle(12,8,2)}${p("M7 5a7 7 0 0 0 0 6m10-6a7 7 0 0 1 0 6M4 2a11 11 0 0 0 0 12m16-14a11 11 0 0 1 0 12")}`;
	if (kind === "skip") return `${p("M6 5v14l9-7zM18 5v14")}`;
	if (kind === "sparkle") return `${p("M4 19 15 8m-8 8 2 2M14 4l1 3 3 1-3 1-1 3-1-3-3-1 3-1zM19 14l.7 2.3L22 17l-2.3.7L19 20l-.7-2.3L16 17l2.3-.7z")}`;
	if (kind === "wand") return `${p("M4 19 15 8m-8 8 2 2")}${p("M17 3l.8 2.2L20 6l-2.2.8L17 9l-.8-2.2L14 6l2.2-.8z")}${rect(4,4,7,7,1)}${line(7.5,4,7.5,11)}${line(4,7.5,11,7.5)}`;
	if (kind === "wrap") return `${p("M4 7h12a4 4 0 0 1 0 8H9m3-3-3 3 3 3M4 11h8M4 15h2")}`;
	if (kind === "gripH") return `${line(5,10,19,10)}${line(5,14,19,14)}${p("m12 7-2-2m2 2 2-2m-2 12-2 2m2-2 2 2")}`;
	if (kind === "gripV") return `${line(10,5,10,19)}${line(14,5,14,19)}${p("m7 12-2-2m2 2-2 2m12-2 2-2m-2 2 2 2")}`;
	if (kind === "unplug") return `${p("M8 3v5m8-5v5M6 8h12v3a6 6 0 0 1-6 6v4M4 4l16 16")}`;
	if (kind === "close" || kind === "minus" || kind === "plus" || kind === "check") return p(marks[kind]);
	if (kind === "left" || kind === "right") return p(kind==="left"?"m15 5-7 7 7 7":"m9 5 7 7-7 7");
	if (kind === "up" || kind === "down") return p(kind==="up"?"m5 15 7-7 7 7":"m5 9 7 7 7-7");
	const generic = {save:"M5 4h12l2 2v14H5zM8 4v6h8V4m-8 12h8", refresh:"M19 8a8 8 0 1 0 1 7m0 0v-5m0 5h-5", pencil:"m5 19 4-1 10-10-3-3L6 15z", play:"M8 5v14l11-7z", link:"M9 15l6-6m-8 9H5a4 4 0 0 1 0-8h3m8 0h3a4 4 0 1 1 0 8h-3", unlink:"M4 4l16 16M7 18H5a4 4 0 0 1 0-8m12-4h2a4 4 0 0 1 2 7", wrench:"M14 6a4 4 0 0 0-5 5L4 16l4 4 5-5a4 4 0 0 0 5-5l-3 1-2-2z"};
	return p(generic[kind] || "M5 5h14v14H5zM8 9h8m-8 3h8m-8 3h5");
}

function svg(kind, name) {
	return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" role="img" aria-labelledby="title"><title id="title">${name.replaceAll("-", " ")}</title><g>${draw(kind)}</g></svg>\n`;
}

await mkdir(path.join(output, "svg"), { recursive: true });
for (const [kind, name] of icons) await writeFile(path.join(output, "svg", `${name}.svg`), svg(kind, name));
const cards = icons.map(([,name]) => `<figure><div><img src="svg/${name}.svg" alt="${name.replaceAll("-", " ")}"></div><figcaption>${name}</figcaption></figure>`).join("");
const html = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>LovelyGit icon review</title><style>:root{color-scheme:dark}body{margin:0;background:#0e0a12;color:#eee6f5;font:14px system-ui;padding:32px}h1{font-size:24px}p{color:#b7aabd}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(150px,1fr));gap:12px}figure{margin:0;border:1px solid #362c3e;border-radius:12px;background:#17111d;padding:14px}figure div{display:grid;place-items:center;height:74px;border-radius:8px;background:#211827}img{width:42px;height:42px;filter:invert(95%) sepia(15%) saturate(850%) hue-rotate(95deg)}figcaption{margin-top:10px;font-size:12px;overflow-wrap:anywhere}</style></head><body><h1>LovelyGit icon concepts</h1><p>${icons.length} review-only SVGs. No application code imports these files.</p><main class="grid">${cards}</main></body></html>`;
await writeFile(path.join(output, "index.html"), html);
