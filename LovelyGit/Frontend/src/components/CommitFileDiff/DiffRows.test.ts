import { describe, expect, it } from "vitest";
import type { CommitFileDiffLine } from "@/generated/types";
import { type DiffDisplayRow, getCombinedLineActionPayload } from "./DiffRows";

describe("getCombinedLineActionPayload", () => {
	it("pairs adjacent deleted and inserted rows into one modified payload", () => {
		const rows = [
			row({
				changeType: "Deleted",
				oldLineNumber: 2,
				text: "old value",
			}),
			row({
				changeType: "Inserted",
				newLineNumber: 2,
				text: "new value",
			}),
		];

		expect(getCombinedLineActionPayload(rows, 0)).toMatchObject({
			changeType: "Modified",
			oldLineNumber: 2,
			newLineNumber: 2,
			oldText: "old value",
			newText: "new value",
		});
		expect(getCombinedLineActionPayload(rows, 1)).toMatchObject({
			changeType: "Modified",
			oldLineNumber: 2,
			newLineNumber: 2,
			oldText: "old value",
			newText: "new value",
		});
	});

	it("leaves real insertions and deletions alone", () => {
		const rows = [
			row({ changeType: "Inserted", newLineNumber: 3, text: "new line" }),
			row({ changeType: "Unchanged", oldLineNumber: 4, newLineNumber: 4 }),
			row({ changeType: "Deleted", oldLineNumber: 5, text: "old line" }),
		];

		expect(getCombinedLineActionPayload(rows, 0)).toBe(lineAt(rows, 0));
		expect(getCombinedLineActionPayload(rows, 2)).toBe(lineAt(rows, 2));
	});
});

function lineAt(rows: DiffDisplayRow[], index: number) {
	const row = rows[index];
	if (row?.kind !== "line") {
		throw new Error(`Expected row ${index} to be a line.`);
	}

	return row.line;
}

function row(line: Partial<CommitFileDiffLine>): DiffDisplayRow {
	return {
		kind: "line",
		line: {
			changeSpans: [],
			changeType: "",
			newChangeSpans: [],
			newLineNumber: null,
			newSyntaxSpans: [],
			newText: "",
			oldChangeSpans: [],
			oldLineNumber: null,
			oldSyntaxSpans: [],
			oldText: "",
			syntaxSpans: [],
			text: "",
			...line,
		},
	};
}
