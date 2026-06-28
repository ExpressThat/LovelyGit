import type { CommitFileDiffLine } from "@/generated/types";

const loadingDiffRows = [
	{ key: "row-a", width: 72 },
	{ key: "row-b", width: 96 },
	{ key: "row-c", width: 96 },
	{ key: "row-d", width: 72 },
	{ key: "row-e", width: 96 },
	{ key: "row-f", width: 96 },
	{ key: "row-g", width: 72 },
	{ key: "row-h", width: 96 },
	{ key: "row-i", width: 96 },
	{ key: "row-j", width: 72 },
	{ key: "row-k", width: 96 },
	{ key: "row-l", width: 96 },
	{ key: "row-m", width: 72 },
	{ key: "row-n", width: 96 },
	{ key: "row-o", width: 96 },
	{ key: "row-p", width: 72 },
];

export function canStageLines(group: string) {
	return group === "Unstaged" || group === "Untracked";
}

export function canUnstageLines(group: string) {
	return group === "Staged";
}

export function workingOldText(line: CommitFileDiffLine) {
	if (line.oldText) {
		return line.oldText;
	}

	return line.changeType === "Deleted" ? line.text : "";
}

export function workingNewText(line: CommitFileDiffLine) {
	if (line.newText) {
		return line.newText;
	}

	return line.changeType === "Inserted" ? line.text : "";
}

export function isSameDiffLine(
	left: CommitFileDiffLine,
	right: CommitFileDiffLine,
) {
	return (
		left.changeType === right.changeType &&
		left.oldLineNumber === right.oldLineNumber &&
		left.newLineNumber === right.newLineNumber &&
		left.oldText === right.oldText &&
		left.newText === right.newText &&
		left.text === right.text
	);
}

export function isChangedLine(line: CommitFileDiffLine) {
	return (
		line.changeType === "Inserted" ||
		line.changeType === "Deleted" ||
		line.changeType === "Modified"
	);
}

export function ModeButton({
	icon,
	isActive,
	label,
	onClick,
}: {
	icon: React.ReactNode;
	isActive: boolean;
	label: string;
	onClick: () => void;
}) {
	return (
		<button
			aria-label={label}
			className={`inline-flex h-7 items-center gap-1 rounded px-2 text-xs text-muted-foreground hover:bg-accent hover:text-accent-foreground ${
				isActive ? "bg-accent font-semibold text-accent-foreground" : ""
			}`}
			onClick={onClick}
			title={label}
			type="button"
		>
			{icon}
			<span>{label}</span>
		</button>
	);
}

export function LoadingDiff() {
	return (
		<div className="space-y-2 p-4">
			{loadingDiffRows.map((row) => (
				<div
					className="h-5 animate-pulse rounded bg-muted"
					key={`working-loading-diff-${row.key}`}
					style={{ width: `${row.width}%` }}
				/>
			))}
		</div>
	);
}

export function folderPrefix(path: string) {
	const lastSlash = Math.max(path.lastIndexOf("/"), path.lastIndexOf("\\"));
	return lastSlash >= 0 ? path.slice(0, lastSlash + 1) : "";
}

export function fileName(path: string) {
	const lastSlash = Math.max(path.lastIndexOf("/"), path.lastIndexOf("\\"));
	return lastSlash >= 0 ? path.slice(lastSlash + 1) : path;
}
