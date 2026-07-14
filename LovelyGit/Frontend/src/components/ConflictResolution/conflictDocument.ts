import type { ConflictResolutionResponse } from "@/generated/types";
import { extractConflictLineRanges } from "./conflictLineRanges";
import {
	type ConflictDocumentSegment,
	type ConflictSegment,
	parseConflictDocument,
	splitLines,
} from "./conflictMarkerParser";

export type {
	ConflictDocumentSegment,
	ConflictSegment,
} from "./conflictMarkerParser";
export { parseConflictDocument, splitLines } from "./conflictMarkerParser";

export type ConflictChoice = {
	resolution: "unresolved" | "selection" | "omit";
	ours: { accepted: boolean; lines: boolean[] };
	theirs: { accepted: boolean; lines: boolean[] };
};

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
	const newline = conflict.result.text?.includes("\r\n") ? "\r\n" : "\n";

	if (parsedConflicts.length === conflict.hunks.length) {
		const hunks = new Map(conflict.hunks.map((hunk) => [hunk.id, hunk]));
		const base = extractConflictLineRanges(
			conflict.base.text ?? "",
			conflict.hunks.map((hunk) => ({
				id: hunk.id,
				startLine: hunk.baseStartLine,
				lineCount: hunk.baseLineCount,
			})),
			newline,
		);
		const ours = extractConflictLineRanges(
			conflict.ours.text ?? "",
			conflict.hunks.map((hunk) => ({
				id: hunk.id,
				startLine: hunk.currentStartLine,
				lineCount: hunk.currentLineCount,
			})),
			newline,
		);
		const theirs = extractConflictLineRanges(
			conflict.theirs.text ?? "",
			conflict.hunks.map((hunk) => ({
				id: hunk.id,
				startLine: hunk.incomingStartLine,
				lineCount: hunk.incomingLineCount,
			})),
			newline,
		);
		for (const segment of parsedConflicts) {
			const hunk = hunks.get(segment.id);
			if (!hunk) continue;
			segment.base = base.get(segment.id) ?? [];
			segment.ours = ours.get(segment.id) ?? [];
			segment.theirs = theirs.get(segment.id) ?? [];
		}
		return parsed;
	}

	if (parsedConflicts.length > 0) return parsed;
	const baseLines = splitLines(conflict.base.text ?? "");
	const currentLines = splitLines(conflict.ours.text ?? "");
	const incomingLines = splitLines(conflict.theirs.text ?? "");
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
	return ["<<<<<<<", "=======", ">>>>>>>"].some((marker) => {
		let index = text.indexOf(marker);
		while (index >= 0) {
			if (index === 0 || text.charCodeAt(index - 1) === 10) return true;
			index = text.indexOf(marker, index + 1);
		}
		return false;
	});
}
