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
	resolution: "unresolved" | "selection" | "omit";
	ours: { accepted: boolean; lines: boolean[] };
	theirs: { accepted: boolean; lines: boolean[] };
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
					resolution: "unresolved",
					ours: {
						accepted: false,
						lines: segment.ours.map(() => false),
					},
					theirs: {
						accepted: false,
						lines: segment.theirs.map(() => false),
					},
				},
			]),
	);
}

export function createConflictDocument(
	conflict: ConflictResolutionResponse,
): ConflictDocumentSegment[] {
	const parsed = parseConflictDocument(conflict.result.text ?? "");
	const parsedConflicts = parsed.filter(
		(segment): segment is ConflictSegment => segment.kind === "conflict",
	);
	const baseLines = splitLines(conflict.base.text ?? "");
	const currentLines = splitLines(conflict.ours.text ?? "");
	const incomingLines = splitLines(conflict.theirs.text ?? "");
	const newline = conflict.result.text?.includes("\r\n") ? "\r\n" : "\n";

	if (parsedConflicts.length === conflict.hunks.length) {
		for (const segment of parsedConflicts) {
			const hunk = conflict.hunks.find(
				(candidate) => candidate.id === segment.id,
			);
			if (!hunk) continue;
			segment.base = normalizeLineEndings(
				sliceRange(baseLines, hunk.baseStartLine, hunk.baseLineCount),
				newline,
			);
			segment.ours = normalizeLineEndings(
				sliceRange(currentLines, hunk.currentStartLine, hunk.currentLineCount),
				newline,
			);
			segment.theirs = normalizeLineEndings(
				sliceRange(
					incomingLines,
					hunk.incomingStartLine,
					hunk.incomingLineCount,
				),
				newline,
			);
		}
		return parsed;
	}

	if (parsedConflicts.length > 0) return parsed;
	return [
		{
			kind: "conflict",
			id: 0,
			base: baseLines,
			ours: currentLines,
			theirs: incomingLines,
			original: conflict.result.text ?? "",
		},
	];
}

export function renderConflictResult(
	segments: ConflictDocumentSegment[],
	choices: Record<number, ConflictChoice>,
) {
	return segments
		.map((segment) => {
			if (segment.kind === "common") return segment.text;
			const choice = choices[segment.id];
			if (!choice || choice.resolution === "unresolved") {
				return segment.base.join("");
			}
			if (choice.resolution === "omit") return "";
			return [
				...(choice.ours.accepted
					? segment.ours.filter((_, index) => choice.ours.lines[index])
					: []),
				...(choice.theirs.accepted
					? segment.theirs.filter((_, index) => choice.theirs.lines[index])
					: []),
			].join("");
		})
		.join("");
}

export function areConflictChoicesResolved(
	segments: ConflictDocumentSegment[],
	choices: Record<number, ConflictChoice>,
) {
	return segments.every(
		(segment) =>
			segment.kind === "common" ||
			choices[segment.id]?.resolution !== "unresolved",
	);
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

export function splitLines(text: string) {
	return text.match(/.*(?:\r\n|\n|$)/g)?.filter(Boolean) ?? [];
}

function sliceRange(lines: string[], oneBasedStart: number, count: number) {
	return lines.slice(
		Math.max(0, oneBasedStart - 1),
		Math.max(0, oneBasedStart - 1) + count,
	);
}

function normalizeLineEndings(lines: string[], newline: string) {
	return lines.map((line) => line.replace(/\r?\n$/, newline));
}

function isMarker(line: string, marker: string) {
	return line.startsWith(marker);
}

import type { ConflictResolutionResponse } from "@/generated/types";
