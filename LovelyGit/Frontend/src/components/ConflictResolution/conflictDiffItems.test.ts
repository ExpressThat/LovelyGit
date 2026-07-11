import { describe, expect, it } from "vitest";
import type { CommitFileDiffLine, ConflictHunk } from "@/generated/types";
import {
	buildConflictDiffItems,
	estimateConflictPaneCodeWidth,
} from "./conflictDiffItems";

const hunk: ConflictHunk = {
	id: 0,
	baseStartLine: 2,
	baseLineCount: 1,
	currentStartLine: 3,
	currentLineCount: 1,
	incomingStartLine: 2,
	incomingLineCount: 1,
};

describe("buildConflictDiffItems", () => {
	it("splits modifications into red base and green source rows", () => {
		const items = buildConflictDiffItems(
			[
				line({
					oldLineNumber: 2,
					newLineNumber: 3,
					oldText: "base",
					newText: "current",
					changeType: "Modified",
				}),
			],
			[hunk],
			"ours",
		);

		expect(
			items.map((item) => (item.kind === "line" ? item.variant : "hunk")),
		).toEqual(["hunk", "deleted", "inserted"]);
		const candidate = items[2];
		expect(candidate.kind === "line" && candidate.candidateIndex).toBe(0);
	});

	it("never turns a deleted-base indicator into selectable content", () => {
		const items = buildConflictDiffItems(
			[
				line({
					oldLineNumber: 2,
					oldText: "removed",
					changeType: "Deleted",
				}),
			],
			[hunk],
			"ours",
		);
		const removed = items[1];
		expect(removed.kind === "line" && removed.variant).toBe("deleted");
		expect(removed.kind === "line" && removed.candidateIndex).toBeNull();
	});

	it("lets short merge panes fit without the full diff viewer's wide minimum", () => {
		const items = buildConflictDiffItems(
			[line({ text: "short", oldText: "short", newText: "short" })],
			[hunk],
			"ours",
		);
		expect(estimateConflictPaneCodeWidth(items)).toBe(80);
	});
});

function line(values: Partial<CommitFileDiffLine>): CommitFileDiffLine {
	return {
		oldLineNumber: null,
		newLineNumber: null,
		oldText: "",
		newText: "",
		text: "",
		changeType: "Unchanged",
		oldSyntaxSpans: [],
		newSyntaxSpans: [],
		syntaxSpans: [],
		oldChangeSpans: [],
		newChangeSpans: [],
		changeSpans: [],
		...values,
	};
}
