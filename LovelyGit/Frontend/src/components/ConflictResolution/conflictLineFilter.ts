import type { CommitFileDiffLine, ConflictHunk } from "@/generated/types";
import type { ConflictSide } from "./conflictDiffItems";
import {
	type ConflictHunkLookup,
	createConflictHunkLookup,
} from "./conflictHunkLookup";

export function filterConflictLines(
	lines: CommitFileDiffLine[],
	hunks: ConflictHunk[],
	side: ConflictSide,
	contextLines: number,
) {
	const keep = new Set<number>();
	const lookup = createConflictHunkLookup(hunks, side);
	for (let index = 0; index < lines.length; index++) {
		if (!isRelevant(lines[index], lookup)) continue;
		const first = Math.max(0, index - contextLines);
		const last = Math.min(lines.length - 1, index + contextLines);
		for (let candidate = first; candidate <= last; candidate++)
			keep.add(candidate);
	}
	return lines.filter((_, index) => keep.has(index));
}

function isRelevant(line: CommitFileDiffLine, lookup: ConflictHunkLookup) {
	if (line.changeType !== "Unchanged") return true;
	return lookup.find(line.oldLineNumber, line.newLineNumber) !== undefined;
}
