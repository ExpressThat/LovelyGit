import type { GitConflictTextLine } from "@/generated/types";

export type ConflictHunk = {
	id: string;
	endLine: number;
	incoming: string[];
	middleLine: number;
	startLine: number;
	current: string[];
};

export function textFromConflictLines(lines: GitConflictTextLine[]) {
	return lines.map((line) => line.text).join("\n");
}

export function findTextSequenceRange(
	lines: Pick<GitConflictTextLine, "text">[],
	expected: string[],
) {
	if (expected.length === 0) return null;
	for (let start = 0; start <= lines.length - expected.length; start++) {
		if (expected.every((text, offset) => lines[start + offset].text === text)) {
			return { end: start + expected.length - 1, start };
		}
	}

	return null;
}

export function parseConflictHunks(text: string): ConflictHunk[] {
	const lines = text.split("\n");
	const hunks: ConflictHunk[] = [];
	for (let index = 0; index < lines.length; index++) {
		if (!lines[index].startsWith("<<<<<<<")) {
			continue;
		}

		const middle = lines.findIndex(
			(line, lineIndex) => lineIndex > index && line.startsWith("======="),
		);
		const end = lines.findIndex(
			(line, lineIndex) => lineIndex > middle && line.startsWith(">>>>>>>"),
		);
		if (middle === -1 || end === -1) {
			continue;
		}

		hunks.push({
			current: lines.slice(index + 1, middle),
			endLine: end + 1,
			id: `${index + 1}:${end + 1}`,
			incoming: lines.slice(middle + 1, end),
			middleLine: middle + 1,
			startLine: index + 1,
		});
		index = end;
	}

	return hunks;
}

export function applyConflictSide(
	text: string,
	hunk: ConflictHunk,
	side: "current" | "incoming",
) {
	const lines = text.split("\n");
	const replacement = side === "current" ? hunk.current : hunk.incoming;
	lines.splice(
		hunk.startLine - 1,
		hunk.endLine - hunk.startLine + 1,
		...replacement,
	);
	return lines.join("\n");
}

export type ConflictChoice = "current" | "incoming" | null;

export function composeConflictResult(
	text: string,
	hunks: ConflictHunk[],
	choices: ConflictChoice[],
) {
	let nextText = text;
	for (let index = hunks.length - 1; index >= 0; index--) {
		const choice = choices[index];
		if (choice === null || choice === undefined) {
			continue;
		}

		nextText = applyConflictSide(nextText, hunks[index], choice);
	}

	return nextText;
}

export function composeConflictResultLines(
	resultLines: GitConflictTextLine[],
	oursLines: GitConflictTextLine[],
	theirsLines: GitConflictTextLine[],
	hunks: ConflictHunk[],
	choices: ConflictChoice[],
) {
	const lines = [...resultLines];
	for (let index = hunks.length - 1; index >= 0; index--) {
		const hunk = hunks[index];
		const choice = choices[index] ?? "current";
		const sourceLines = choice === "current" ? oursLines : theirsLines;
		const expected = choice === "current" ? hunk.current : hunk.incoming;
		const range = findTextSequenceRange(sourceLines, expected);
		const replacement = range
			? sourceLines.slice(range.start, range.end + 1)
			: expected.map((text) => ({
					lineNumber: 0,
					markerKind: "",
					syntaxSpans: [],
					text,
				}));

		lines.splice(
			hunk.startLine - 1,
			hunk.endLine - hunk.startLine + 1,
			...replacement,
		);
	}

	return lines.map((line, index) => ({ ...line, lineNumber: index + 1 }));
}
