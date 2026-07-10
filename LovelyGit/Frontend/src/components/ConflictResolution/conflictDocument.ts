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

export type ConflictChoice = {
	mode: "unresolved" | "custom";
	ours: boolean[];
	theirs: boolean[];
};

export function parseConflictDocument(text: string): ConflictDocumentSegment[] {
	const lines = splitLines(text);
	const segments: ConflictDocumentSegment[] = [];
	let common = "";
	let conflictId = 0;

	for (let index = 0; index < lines.length; index += 1) {
		if (!isMarker(lines[index], "<<<<<<<")) {
			common += lines[index];
			continue;
		}

		const parsed = readConflict(lines, index, conflictId);
		if (!parsed) {
			common += lines[index];
			continue;
		}

		if (common) {
			segments.push({ kind: "common", text: common });
			common = "";
		}
		segments.push(parsed.segment);
		conflictId += 1;
		index = parsed.endIndex;
	}

	if (common || segments.length === 0) {
		segments.push({ kind: "common", text: common });
	}
	return segments;
}

export function createConflictChoices(
	segments: ConflictDocumentSegment[],
): Record<number, ConflictChoice> {
	return Object.fromEntries(
		segments
			.filter(
				(segment): segment is ConflictSegment => segment.kind === "conflict",
			)
			.map((segment) => [
				segment.id,
				{
					mode: "unresolved",
					ours: segment.ours.map(() => false),
					theirs: segment.theirs.map(() => false),
				},
			]),
	);
}

export function renderConflictResult(
	segments: ConflictDocumentSegment[],
	choices: Record<number, ConflictChoice>,
) {
	return segments
		.map((segment) => {
			if (segment.kind === "common") return segment.text;
			const choice = choices[segment.id];
			if (!choice || choice.mode === "unresolved") return segment.original;
			return [
				...segment.ours.filter((_, index) => choice.ours[index]),
				...segment.theirs.filter((_, index) => choice.theirs[index]),
			].join("");
		})
		.join("");
}

export function hasConflictMarkers(text: string) {
	return text
		.split(/\r?\n/)
		.some((line) =>
			["<<<<<<<", "=======", ">>>>>>>"].some((marker) =>
				line.startsWith(marker),
			),
		);
}

function readConflict(lines: string[], start: number, id: number) {
	const ours: string[] = [];
	const base: string[] = [];
	const theirs: string[] = [];
	let target = ours;
	let foundSeparator = false;

	for (let index = start + 1; index < lines.length; index += 1) {
		const line = lines[index];
		if (isMarker(line, "|||||||")) {
			target = base;
		} else if (isMarker(line, "=======")) {
			target = theirs;
			foundSeparator = true;
		} else if (isMarker(line, ">>>>>>>")) {
			if (!foundSeparator) return null;
			return {
				endIndex: index,
				segment: {
					kind: "conflict" as const,
					id,
					ours,
					base,
					theirs,
					original: lines.slice(start, index + 1).join(""),
				},
			};
		} else {
			target.push(line);
		}
	}
	return null;
}

function splitLines(text: string) {
	return text.match(/.*(?:\r\n|\n|$)/g)?.filter(Boolean) ?? [];
}

function isMarker(line: string, marker: string) {
	return line.startsWith(marker);
}
