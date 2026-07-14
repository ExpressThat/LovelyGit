export type ConflictSegment = {
	kind: "conflict";
	id: number;
	ours: string[];
	theirs: string[];
	base: string[];
	original: string;
};

export type ConflictDocumentSegment =
	| { kind: "common"; text: string }
	| ConflictSegment;

export function parseConflictDocument(text: string): ConflictDocumentSegment[] {
	const segments: ConflictDocumentSegment[] = [];
	let commonStart = 0;
	let searchStart = 0;
	let conflictId = 0;

	while (searchStart < text.length) {
		const openingStart = findMarker(text, "<<<<<<<", searchStart);
		if (openingStart < 0) break;
		const parsed = readConflict(text, openingStart, conflictId);
		if (!parsed) {
			searchStart = lineEnd(text, openingStart);
			continue;
		}
		if (openingStart > commonStart) {
			segments.push({
				kind: "common",
				text: text.slice(commonStart, openingStart),
			});
		}
		segments.push(parsed.segment);
		conflictId++;
		commonStart = parsed.end;
		searchStart = parsed.end;
	}

	if (commonStart < text.length || segments.length === 0) {
		segments.push({ kind: "common", text: text.slice(commonStart) });
	}
	return segments;
}

export function splitLines(text: string) {
	const lines: string[] = [];
	let start = 0;
	while (start < text.length) {
		const newline = text.indexOf("\n", start);
		const end = newline < 0 ? text.length : newline + 1;
		lines.push(text.slice(start, end));
		start = end;
	}
	return lines;
}

function readConflict(text: string, start: number, id: number) {
	const openingEnd = lineEnd(text, start);
	let oursEnd = -1;
	let baseStart = -1;
	let baseEnd = -1;
	let theirsStart = -1;
	let cursor = openingEnd;

	while (cursor < text.length) {
		const end = lineEnd(text, cursor);
		if (text.startsWith("|||||||", cursor) && theirsStart < 0) {
			oursEnd = cursor;
			baseStart = end;
		} else if (text.startsWith("=======", cursor)) {
			if (theirsStart >= 0) return null;
			if (baseStart < 0) oursEnd = cursor;
			else baseEnd = cursor;
			theirsStart = end;
		} else if (text.startsWith(">>>>>>>", cursor)) {
			if (theirsStart < 0) return null;
			return {
				end,
				segment: {
					kind: "conflict" as const,
					id,
					ours: splitLines(text.slice(openingEnd, oursEnd)),
					base: baseStart < 0 ? [] : splitLines(text.slice(baseStart, baseEnd)),
					theirs: splitLines(text.slice(theirsStart, cursor)),
					original: text.slice(start, end),
				},
			};
		}
		cursor = end;
	}
	return null;
}

function findMarker(text: string, marker: string, start: number) {
	let index = text.indexOf(marker, start);
	while (index >= 0) {
		if (index === 0 || text.charCodeAt(index - 1) === 10) return index;
		index = text.indexOf(marker, index + 1);
	}
	return -1;
}

function lineEnd(text: string, start: number) {
	const newline = text.indexOf("\n", start);
	return newline < 0 ? text.length : newline + 1;
}
