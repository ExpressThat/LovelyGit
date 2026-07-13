import type { CommitFileDiffLine } from "@/generated/types";

type SyntaxSpanTuple = [number, number, string];
type ChangeSpanTuple = [number, number, string];
export type DeltaReferenceTuple = [
	number | null,
	number | null,
	number | string,
	SyntaxSpanTuple[]?,
	SyntaxSpanTuple[]?,
	SyntaxSpanTuple[]?,
	ChangeSpanTuple[]?,
	ChangeSpanTuple[]?,
	ChangeSpanTuple[]?,
];

export function decodeDeltaReferenceLines(
	tuples: DeltaReferenceTuple[],
	oldText: string,
	newText: string,
	combined = false,
) {
	return decodeDeltaReferenceLineArrays(
		tuples,
		textLines(oldText),
		textLines(newText),
		combined,
	);
}

export function decodeDeltaReferenceLineArrays(
	tuples: DeltaReferenceTuple[],
	oldLines: string[],
	newLines: string[],
	combined: boolean,
) {
	let previousOld = 0;
	let previousNew = 0;
	const lines: CommitFileDiffLine[] = [];
	for (const tuple of tuples) {
		const oldLineNumber = applyDelta(tuple[0], previousOld);
		const newLineNumber = applyDelta(tuple[1], previousNew);
		if (oldLineNumber != null) previousOld = oldLineNumber;
		if (newLineNumber != null) previousNew = newLineNumber;
		const oldLineText = lineAt(oldLines, oldLineNumber);
		const newLineText = lineAt(newLines, newLineNumber);
		const resolvedChangeType = changeType(tuple[2]);
		const line: CommitFileDiffLine = {
			oldLineNumber,
			newLineNumber,
			oldText: oldLineText,
			newText: newLineText,
			text: "",
			changeType: resolvedChangeType,
			oldSyntaxSpans: (tuple[3] ?? []).map(toSyntaxSpan),
			newSyntaxSpans: (tuple[4] ?? []).map(toSyntaxSpan),
			syntaxSpans: (tuple[5] ?? []).map(toSyntaxSpan),
			oldChangeSpans: (tuple[6] ?? []).map(toChangeSpan),
			newChangeSpans: (tuple[7] ?? []).map(toChangeSpan),
			changeSpans: (tuple[8] ?? []).map(toChangeSpan),
		};
		if (
			combined &&
			resolvedChangeType === "Modified" &&
			oldLineNumber != null &&
			newLineNumber != null
		) {
			lines.push(
				{
					...line,
					newLineNumber: null,
					newText: "",
					text: oldLineText,
					changeType: "Deleted",
					syntaxSpans: line.oldSyntaxSpans,
					changeSpans: line.oldChangeSpans,
				},
				{
					...line,
					oldLineNumber: null,
					oldText: "",
					text: newLineText,
					changeType: "Inserted",
					syntaxSpans: line.newSyntaxSpans,
					changeSpans: line.newChangeSpans,
				},
			);
		} else {
			line.text = combined
				? resolvedChangeType === "Inserted" || resolvedChangeType === "Added"
					? newLineText
					: oldLineText
				: "";
			lines.push(line);
		}
	}
	return lines;
}

function applyDelta(delta: number | null, previous: number) {
	return delta == null ? null : previous + delta;
}

function changeType(value: number | string) {
	if (typeof value === "string") return value;
	return (
		["Unchanged", "Modified", "Deleted", "Inserted", "Added", "Imaginary"][
			value
		] ?? ""
	);
}

function textLines(text: string) {
	return text.replaceAll("\r\n", "\n").split("\n");
}

function lineAt(lines: string[], oneBasedIndex: number | null) {
	return oneBasedIndex == null ? "" : (lines[oneBasedIndex - 1] ?? "");
}

function toSyntaxSpan([start, length, scope]: SyntaxSpanTuple) {
	return { start, length, scope };
}

function toChangeSpan([start, length, changeType]: ChangeSpanTuple) {
	return { start, length, changeType };
}
