import { describe, expect, it } from "vitest";
import type { CommitFileDiffLine, ConflictHunk } from "@/generated/types";
import { filterConflictLines } from "./conflictLineFilter";

describe("filterConflictLines", () => {
	it("keeps changed rows, context, and unchanged conflict candidates", () => {
		const lines = ["one", "two", "candidate", "four", "changed", "six"].map(
			(text, index) =>
				line(index + 1, text, index === 4 ? "Modified" : "Unchanged"),
		);
		const hunk: ConflictHunk = {
			id: 0,
			baseStartLine: 3,
			baseLineCount: 1,
			currentStartLine: 3,
			currentLineCount: 1,
			incomingStartLine: 3,
			incomingLineCount: 1,
		};

		expect(
			filterConflictLines(lines, [hunk], "ours", 1).map((row) => row.text),
		).toEqual(["two", "candidate", "four", "changed", "six"]);
	});
});

function line(
	number: number,
	text: string,
	changeType: string,
): CommitFileDiffLine {
	return {
		oldLineNumber: number,
		newLineNumber: number,
		oldText: text,
		newText: text,
		text,
		changeType,
		oldSyntaxSpans: [],
		newSyntaxSpans: [],
		syntaxSpans: [],
		oldChangeSpans: [],
		newChangeSpans: [],
		changeSpans: [],
	};
}
