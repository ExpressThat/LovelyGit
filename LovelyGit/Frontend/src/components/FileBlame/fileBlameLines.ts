import type { FileBlameHunk } from "@/generated/types";

export function splitBlameLines(content: string) {
	if (!content) return [];
	const lines = content.replaceAll("\r\n", "\n").split("\n");
	if (content.endsWith("\n")) lines.pop();
	return lines;
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
