import { describe, expect, it } from "vitest";
import type { CommitFileDiffLine } from "@/generated/types";
import {
	type DiffDisplayRow,
	getCombinedLineActionPayload,
	getContextualDiffRows,
} from "./DiffRows";

describe("getContextualDiffRows", () => {
	it("materializes only sparse changed ranges and their context", () => {
		const lines = Array.from({ length: 10_000 }, () => line());
		lines[100] = line({ changeType: "Modified" });
		lines[9_000] = line({ changeType: "Inserted" });

		const rows = getContextualDiffRows(lines, 1);

		expect(rows).toHaveLength(7);
		expect(rows[3]).toEqual({ kind: "separator" });
		expect(rows[0]).toEqual({ kind: "line", line: lines[99] });
		expect(rows[6]).toEqual({ kind: "line", line: lines[9_001] });
	});

	it("merges overlapping context into one continuous range", () => {
		const lines = Array.from({ length: 12 }, () => line());
		lines[4] = line({ changeType: "Deleted" });
		lines[7] = line({ changeType: "Inserted" });

		const rows = getContextualDiffRows(lines, 2);

		expect(rows).toHaveLength(8);
		expect(rows.every((item) => item.kind === "line")).toBe(true);
		expect(rows[0]).toEqual({ kind: "line", line: lines[2] });
		expect(rows[7]).toEqual({ kind: "line", line: lines[9] });
	});
});

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
		line: lineValue(line),
	};
}

function line(line: Partial<CommitFileDiffLine> = {}) {
	return lineValue(line);
}

function lineValue(line: Partial<CommitFileDiffLine>) {
	return {
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
	};
}
