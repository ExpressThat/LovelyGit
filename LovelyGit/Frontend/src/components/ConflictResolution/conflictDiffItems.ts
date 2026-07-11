import type {
	CommitFileDiffChangeSpan,
	CommitFileDiffLine,
	CommitFileDiffSyntaxSpan,
	ConflictHunk,
} from "@/generated/types";

export type ConflictSide = "ours" | "theirs";

export type ConflictDiffItem =
	| { kind: "hunk"; hunk: ConflictHunk }
	| {
			kind: "line";
			key: string;
			baseLine: number | null;
			sourceLine: number | null;
			text: string;
			variant: "deleted" | "inserted" | "plain";
			syntaxSpans: CommitFileDiffSyntaxSpan[];
			changeSpans: CommitFileDiffChangeSpan[];
			hunkId: number | null;
			candidateIndex: number | null;
	  };

export function buildConflictDiffItems(
	lines: CommitFileDiffLine[],
	hunks: ConflictHunk[],
	side: ConflictSide,
): ConflictDiffItem[] {
	const expanded = lines.flatMap(expandLine);
	const items: ConflictDiffItem[] = [];
	const shownHunks = new Set<number>();

	for (const row of expanded) {
		const hunk = findHunk(row.baseLine, row.sourceLine, hunks, side);
		if (hunk && !shownHunks.has(hunk.id)) {
			items.push({ kind: "hunk", hunk });
			shownHunks.add(hunk.id);
		}
		items.push({
			...row,
			hunkId: hunk?.id ?? null,
			candidateIndex: candidateIndex(row.sourceLine, hunk, side),
		});
	}

	for (const hunk of hunks) {
		if (!shownHunks.has(hunk.id)) items.push({ kind: "hunk", hunk });
	}
	return items;
}

export function estimateConflictPaneCodeWidth(items: ConflictDiffItem[]) {
	let longest = 0;
	for (const item of items) {
		if (item.kind === "line") longest = Math.max(longest, item.text.length);
		if (longest >= 6_617) break;
	}
	return Math.min(48_000, Math.max(80, longest * 7.25 + 32));
}

function expandLine(line: CommitFileDiffLine) {
	if (line.changeType === "Modified") {
		return [oldRow(line, "deleted"), newRow(line, "inserted")];
	}
	if (line.changeType === "Deleted") return [oldRow(line, "deleted")];
	if (line.changeType === "Inserted" || line.changeType === "Added") {
		return [newRow(line, "inserted")];
	}
	return [newRow(line, "plain", line.oldLineNumber)];
}

function oldRow(
	line: CommitFileDiffLine,
	variant: "deleted",
): Omit<
	Extract<ConflictDiffItem, { kind: "line" }>,
	"hunkId" | "candidateIndex"
> {
	return {
		kind: "line",
		key: `old:${line.oldLineNumber}:${line.oldText}`,
		baseLine: line.oldLineNumber,
		sourceLine: null,
		text: line.oldText || line.text,
		variant,
		syntaxSpans: line.oldSyntaxSpans,
		changeSpans: line.oldChangeSpans,
	};
}

function newRow(
	line: CommitFileDiffLine,
	variant: "inserted" | "plain",
	baseLine: number | null = null,
): Omit<
	Extract<ConflictDiffItem, { kind: "line" }>,
	"hunkId" | "candidateIndex"
> {
	return {
		kind: "line",
		key: `new:${line.newLineNumber}:${line.newText || line.text}`,
		baseLine,
		sourceLine: line.newLineNumber,
		text: line.newText || line.text,
		variant,
		syntaxSpans:
			line.newSyntaxSpans.length > 0 ? line.newSyntaxSpans : line.syntaxSpans,
		changeSpans:
			line.newChangeSpans.length > 0 ? line.newChangeSpans : line.changeSpans,
	};
}

function findHunk(
	baseLine: number | null,
	sourceLine: number | null,
	hunks: ConflictHunk[],
	side: ConflictSide,
) {
	return hunks.find((hunk) => {
		const sourceStart =
			side === "ours" ? hunk.currentStartLine : hunk.incomingStartLine;
		const sourceCount =
			side === "ours" ? hunk.currentLineCount : hunk.incomingLineCount;
		return (
			inRange(baseLine, hunk.baseStartLine, hunk.baseLineCount) ||
			inRange(sourceLine, sourceStart, sourceCount)
		);
	});
}

function candidateIndex(
	sourceLine: number | null,
	hunk: ConflictHunk | undefined,
	side: ConflictSide,
) {
	if (!hunk || sourceLine == null) return null;
	const start =
		side === "ours" ? hunk.currentStartLine : hunk.incomingStartLine;
	const count =
		side === "ours" ? hunk.currentLineCount : hunk.incomingLineCount;
	return inRange(sourceLine, start, count) ? sourceLine - start : null;
}

function inRange(value: number | null, start: number, count: number) {
	return value != null && count > 0 && value >= start && value < start + count;
}
