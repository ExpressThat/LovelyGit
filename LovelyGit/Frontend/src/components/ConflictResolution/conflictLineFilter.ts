import type { CommitFileDiffLine, ConflictHunk } from "@/generated/types";
import type { ConflictSide } from "./conflictDiffItems";

export function filterConflictLines(
	lines: CommitFileDiffLine[],
	hunks: ConflictHunk[],
	side: ConflictSide,
	contextLines: number,
) {
	const keep = new Set<number>();
	for (let index = 0; index < lines.length; index++) {
		if (!isRelevant(lines[index], hunks, side)) continue;
		const first = Math.max(0, index - contextLines);
		const last = Math.min(lines.length - 1, index + contextLines);
		for (let candidate = first; candidate <= last; candidate++)
			keep.add(candidate);
	}
	return lines.filter((_, index) => keep.has(index));
}

function isRelevant(
	line: CommitFileDiffLine,
	hunks: ConflictHunk[],
	side: ConflictSide,
) {
	if (line.changeType !== "Unchanged") return true;
	return hunks.some((hunk) => {
		const sourceStart =
			side === "ours" ? hunk.currentStartLine : hunk.incomingStartLine;
		const sourceCount =
			side === "ours" ? hunk.currentLineCount : hunk.incomingLineCount;
		return (
			inRange(line.oldLineNumber, hunk.baseStartLine, hunk.baseLineCount) ||
			inRange(line.newLineNumber, sourceStart, sourceCount)
		);
	});
}

function inRange(value: number | null, start: number, count: number) {
	return value != null && count > 0 && value >= start && value < start + count;
}
