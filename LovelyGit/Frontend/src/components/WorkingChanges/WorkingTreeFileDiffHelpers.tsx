import type { CommitFileDiffLine } from "@/generated/types";


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

export function isSameDiffLine(left: CommitFileDiffLine, right: CommitFileDiffLine) {
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
			{Array.from({ length: 16 }, (_, index) => (
				<div
					className="h-5 animate-pulse rounded bg-muted"
					key={`working-loading-diff-row-${index}`}
					style={{ width: `${index % 3 === 0 ? 72 : 96}%` }}
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
