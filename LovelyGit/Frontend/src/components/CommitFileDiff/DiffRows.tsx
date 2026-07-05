import { Minus, Plus } from "lucide-react";
import type { CommitFileDiffLine } from "@/generated/types";

export type DiffLineAction = {
	kind: "stage" | "unstage";
	onClick: (line: CommitFileDiffLine) => void;
};

export type DiffDisplayRow =
	| { kind: "line"; line: CommitFileDiffLine }
	| { kind: "separator" };

export function DiffChunkSeparator() {
	return (
		<div className="flex min-h-[18px] select-none items-center border-y border-dashed border-border bg-card/70 px-3 text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
			<span className="h-px flex-1 bg-border" />
			<span className="px-2">Hidden unchanged lines</span>
			<span className="h-px flex-1 bg-border" />
		</div>
	);
}

export function DiffLineActionButton({
	action,
	disabled = false,
	line,
}: {
	action: DiffLineAction;
	disabled?: boolean;
	line: CommitFileDiffLine;
}) {
	const Icon = action.kind === "stage" ? Plus : Minus;
	const colorClass =
		action.kind === "stage"
			? "text-emerald-600 hover:bg-emerald-500/15 hover:text-emerald-500 dark:text-emerald-300"
			: "text-amber-600 hover:bg-amber-500/15 hover:text-amber-500 dark:text-amber-300";
	return (
		<button
			aria-label={action.kind === "stage" ? "Stage line" : "Unstage line"}
			className={`flex min-h-[18px] items-center justify-center border-l opacity-75 hover:opacity-100 disabled:pointer-events-none disabled:opacity-35 ${colorClass}`}
			disabled={disabled}
			onClick={() => action.onClick(line)}
			title={action.kind === "stage" ? "Stage line" : "Unstage line"}
			type="button"
		>
			<Icon aria-hidden="true" size={12} strokeWidth={3} />
		</button>
	);
}

export function getSideBySideLineAction(
	line: CommitFileDiffLine,
	side: "old" | "new",
	onStageLine?: (line: CommitFileDiffLine) => void,
	onUnstageLine?: (line: CommitFileDiffLine) => void,
): DiffLineAction | undefined {
	if (line.changeType === "Deleted") {
		return side === "old"
			? getAvailableLineAction(onStageLine, onUnstageLine)
			: undefined;
	}

	if (line.changeType === "Inserted" || line.changeType === "Modified") {
		return side === "new"
			? getAvailableLineAction(onStageLine, onUnstageLine)
			: undefined;
	}

	return undefined;
}

export function getCombinedLineAction(
	line: CommitFileDiffLine,
	onStageLine?: (line: CommitFileDiffLine) => void,
	onUnstageLine?: (line: CommitFileDiffLine) => void,
): DiffLineAction | undefined {
	if (
		line.changeType !== "Deleted" &&
		line.changeType !== "Inserted" &&
		line.changeType !== "Modified"
	) {
		return undefined;
	}

	return getAvailableLineAction(onStageLine, onUnstageLine);
}

export function getCombinedLineActionPayload(
	rows: DiffDisplayRow[],
	index: number,
): CommitFileDiffLine | null {
	const row = rows[index];
	if (!row || row.kind !== "line") {
		return null;
	}

	const line = row.line;
	if (line.changeType === "Deleted") {
		const next = rows[index + 1];
		if (
			next?.kind === "line" &&
			next.line.changeType === "Inserted" &&
			line.oldLineNumber != null &&
			next.line.newLineNumber != null
		) {
			return {
				...line,
				changeType: "Modified",
				newLineNumber: next.line.newLineNumber,
				newText: workingLineText(next.line),
				oldText: workingLineText(line),
			};
		}
	}

	if (line.changeType === "Inserted") {
		const previous = rows[index - 1];
		if (
			previous?.kind === "line" &&
			previous.line.changeType === "Deleted" &&
			previous.line.oldLineNumber != null &&
			line.newLineNumber != null
		) {
			return {
				...line,
				changeType: "Modified",
				oldLineNumber: previous.line.oldLineNumber,
				oldText: workingLineText(previous.line),
				newText: workingLineText(line),
			};
		}
	}

	return line;
}

function getAvailableLineAction(
	onStageLine?: (line: CommitFileDiffLine) => void,
	onUnstageLine?: (line: CommitFileDiffLine) => void,
): DiffLineAction | undefined {
	if (onStageLine) {
		return { kind: "stage", onClick: onStageLine };
	}

	if (onUnstageLine) {
		return { kind: "unstage", onClick: onUnstageLine };
	}

	return undefined;
}

export function getContextualDiffRows(
	lines: CommitFileDiffLine[],
	contextLines: number,
): DiffDisplayRow[] {
	if (lines.length === 0) {
		return [];
	}

	const includedIndexes = new Set<number>();
	for (let index = 0; index < lines.length; index++) {
		if (!isDiffChangedLine(lines[index])) {
			continue;
		}

		const start = Math.max(0, index - contextLines);
		const end = Math.min(lines.length - 1, index + contextLines);
		for (let contextIndex = start; contextIndex <= end; contextIndex++) {
			includedIndexes.add(contextIndex);
		}
	}

	if (includedIndexes.size === 0 || includedIndexes.size === lines.length) {
		return lines.map((line) => ({ kind: "line", line }));
	}

	const rows: DiffDisplayRow[] = [];
	let previousIncludedIndex: number | null = null;
	for (let index = 0; index < lines.length; index++) {
		if (!includedIndexes.has(index)) {
			continue;
		}

		if (previousIncludedIndex !== null && index > previousIncludedIndex + 1) {
			rows.push({ kind: "separator" });
		}

		rows.push({ kind: "line", line: lines[index] });
		previousIncludedIndex = index;
	}

	return rows;
}

function isDiffChangedLine(line: CommitFileDiffLine) {
	return (
		line.changeType === "Deleted" ||
		line.changeType === "Inserted" ||
		line.changeType === "Modified"
	);
}

function workingLineText(line: CommitFileDiffLine) {
	return line.text || line.newText || line.oldText;
}
