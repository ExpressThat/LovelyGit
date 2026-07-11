import { ListMinus, ListPlus } from "lucide-react";
import type { CommitFileDiffLine } from "@/generated/types";

export type DiffHunkAction = {
	kind: "stage" | "unstage";
	lines: CommitFileDiffLine[];
	onClick: (lines: CommitFileDiffLine[]) => void;
};

export function buildDiffHunkLookup(
	lines: CommitFileDiffLine[],
	contextLines: number,
) {
	const lookup = new Map<CommitFileDiffLine, CommitFileDiffLine[]>();
	const maxGap = Math.max(0, contextLines) * 2 + 1;
	let previousChangedIndex = -1;
	let hunk: CommitFileDiffLine[] = [];
	for (let index = 0; index < lines.length; index++) {
		const line = lines[index];
		if (!isChanged(line)) continue;
		if (hunk.length > 0 && index - previousChangedIndex > maxGap) {
			lookup.set(hunk[0], hunk);
			hunk = [];
		}

		hunk.push(line);
		previousChangedIndex = index;
	}

	if (hunk.length > 0) lookup.set(hunk[0], hunk);
	return lookup;
}

export function getDiffHunkAction(
	line: CommitFileDiffLine,
	lookup: Map<CommitFileDiffLine, CommitFileDiffLine[]>,
	onStageHunk?: (lines: CommitFileDiffLine[]) => void,
	onUnstageHunk?: (lines: CommitFileDiffLine[]) => void,
): DiffHunkAction | undefined {
	const lines = lookup.get(line);
	if (!lines) return undefined;
	if (onStageHunk) return { kind: "stage", lines, onClick: onStageHunk };
	if (onUnstageHunk) return { kind: "unstage", lines, onClick: onUnstageHunk };
	return undefined;
}

export function getSideDiffHunkAction(
	line: CommitFileDiffLine,
	side: "old" | "new",
	lookup: Map<CommitFileDiffLine, CommitFileDiffLine[]>,
	onStageHunk?: (lines: CommitFileDiffLine[]) => void,
	onUnstageHunk?: (lines: CommitFileDiffLine[]) => void,
) {
	const actionable =
		line.changeType === "Deleted"
			? side === "old"
			: (line.changeType === "Inserted" || line.changeType === "Modified") &&
				side === "new";
	return actionable
		? getDiffHunkAction(line, lookup, onStageHunk, onUnstageHunk)
		: undefined;
}

export function DiffHunkActionButton({
	action,
	disabled,
}: {
	action: DiffHunkAction;
	disabled: boolean;
}) {
	const isStage = action.kind === "stage";
	const Icon = isStage ? ListPlus : ListMinus;
	const label = isStage ? "Stage hunk" : "Unstage hunk";
	return (
		<button
			aria-label={label}
			className={`absolute right-8 top-0 z-10 flex h-[18px] w-6 items-center justify-center rounded-l border border-r-0 bg-card/95 shadow-sm hover:bg-accent disabled:pointer-events-none disabled:opacity-35 ${isStage ? "text-emerald-500" : "text-amber-500"}`}
			disabled={disabled}
			onClick={() => action.onClick(action.lines)}
			title={`${label} (${action.lines.length} changed ${action.lines.length === 1 ? "line" : "lines"})`}
			type="button"
		>
			<Icon aria-hidden="true" size={12} strokeWidth={2.5} />
		</button>
	);
}

function isChanged(line: CommitFileDiffLine) {
	return (
		line.changeType === "Inserted" ||
		line.changeType === "Deleted" ||
		line.changeType === "Modified"
	);
}
