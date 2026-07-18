import type { FileBlameHunk } from "@/generated/types";

export function buildBlameLineStarts(content: string, lineCount: number) {
	const starts = new Uint32Array(lineCount + 1);
	let line = 1;
	for (let index = 0; index < content.length && line < lineCount; index++) {
		if (content.charCodeAt(index) === 10) starts[line++] = index + 1;
	}
	starts[lineCount] = content.length;
	return starts;
}

export function readBlameLine(
	content: string,
	starts: Uint32Array,
	index: number,
) {
	const start = starts[index] ?? 0;
	let end = starts[index + 1] ?? content.length;
	if (end > start && content.charCodeAt(end - 1) === 10) end--;
	if (end > start && content.charCodeAt(end - 1) === 13) end--;
	return content.slice(start, end);
}

export function findBlameHunk(hunks: FileBlameHunk[], lineNumber: number) {
	let low = 0;
	let high = hunks.length - 1;
	while (low <= high) {
		const middle = (low + high) >>> 1;
		const hunk = hunks[middle];
		if (!hunk) return null;
		if (lineNumber < hunk.startLine) high = middle - 1;
		else if (lineNumber >= hunk.startLine + hunk.lineCount) low = middle + 1;
		else return hunk;
	}

	return null;
}
