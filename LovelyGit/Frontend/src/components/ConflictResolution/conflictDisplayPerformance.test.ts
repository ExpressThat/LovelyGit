import { describe, expect, it } from "vitest";
import type { CommitFileDiffLine, ConflictHunk } from "@/generated/types";
import { buildConflictDiffItems } from "./conflictDiffItems";
import { filterConflictLines } from "./conflictLineFilter";

describe("conflict display performance", () => {
	it("keeps many-conflict Changes and Full file switches responsive", () => {
		const lines = createLines(20_000);
		const hunks = createHunks(500);
		let renderedItems = 0;
		const startedAt = performance.now();
		for (let iteration = 0; iteration < 5; iteration++) {
			const changes = filterConflictLines(lines, hunks, "ours", 3);
			renderedItems += buildConflictDiffItems(changes, hunks, "ours").length;
			renderedItems += buildConflictDiffItems(lines, hunks, "ours").length;
		}
		const elapsed = performance.now() - startedAt;

		console.info(`Conflict display switches: ${elapsed.toFixed(2)} ms`);
		expect(renderedItems).toBeGreaterThan(100_000);
		expect(elapsed).toBeLessThan(150);
	});
});

function createLines(count: number): CommitFileDiffLine[] {
	return Array.from({ length: count }, (_, index) => {
		const number = index + 1;
		const text = `line ${number}`;
		return {
			oldLineNumber: number,
			newLineNumber: number,
			oldText: text,
			newText: text,
			text,
			changeType: "Unchanged",
			oldSyntaxSpans: [],
			newSyntaxSpans: [],
			syntaxSpans: [],
			oldChangeSpans: [],
			newChangeSpans: [],
			changeSpans: [],
		};
	});
}

function createHunks(count: number): ConflictHunk[] {
	return Array.from({ length: count }, (_, id) => {
		const line = id * 40 + 20;
		return {
			id,
			baseStartLine: line,
			baseLineCount: 1,
			currentStartLine: line,
			currentLineCount: 1,
			incomingStartLine: line,
			incomingLineCount: 1,
		};
	});
}
