export type ConflictLineRange = {
	id: number;
	startLine: number;
	lineCount: number;
};

export function extractConflictLineRanges(
	text: string,
	ranges: ConflictLineRange[],
	newline: "\n" | "\r\n",
) {
	const result = new Map<number, string[]>(
		ranges.map((range) => [range.id, []]),
	);
	const pending = ranges
		.filter((range) => range.lineCount > 0)
		.map((range) => ({ ...range, startLine: Math.max(1, range.startLine) }));
	pending.sort((left, right) => left.startLine - right.startLine);
	if (pending.length === 0 || text.length === 0) return result;

	let offset = 0;
	let lineNumber = 1;
	let pendingIndex = 0;
	const active: ConflictLineRange[] = [];
	const finalLine = Math.max(
		...pending.map((range) => range.startLine + range.lineCount - 1),
	);
	while (offset < text.length && lineNumber <= finalLine) {
		while (
			pendingIndex < pending.length &&
			pending[pendingIndex].startLine <= lineNumber
		) {
			active.push(pending[pendingIndex]);
			pendingIndex += 1;
		}

		const newlineAt = text.indexOf("\n", offset);
		const end = newlineAt < 0 ? text.length : newlineAt + 1;
		for (let index = active.length - 1; index >= 0; index -= 1) {
			const range = active[index];
			const rangeEnd = range.startLine + range.lineCount - 1;
			if (lineNumber <= rangeEnd) {
				result
					.get(range.id)
					?.push(normalizeLine(text.slice(offset, end), newline));
			}
			if (lineNumber >= rangeEnd) active.splice(index, 1);
		}

		offset = end;
		lineNumber += 1;
	}
	return result;
}

function normalizeLine(line: string, newline: "\n" | "\r\n") {
	if (!line.endsWith("\n")) return line;
	const contentEnd = line.endsWith("\r\n") ? line.length - 2 : line.length - 1;
	return `${line.slice(0, contentEnd)}${newline}`;
}
