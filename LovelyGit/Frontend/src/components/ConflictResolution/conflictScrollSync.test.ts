import { describe, expect, it } from "vitest";
import type { ConflictDiffItem } from "./conflictDiffItems";
import {
	buildConflictScrollRows,
	findConflictScrollRow,
	findMeasurementAtOffset,
} from "./conflictScrollSync";

describe("conflictScrollSync", () => {
	it("aligns unequal change lists by their shared base line", () => {
		const sparse = buildConflictScrollRows(
			[line(49_999), hunk(50_000), line(50_000), line(50_001)],
			"ours",
		);
		const dense = buildConflictScrollRows(
			[line(1_000), line(2_000), hunk(50_000), line(50_000), line(100_000)],
			"theirs",
		);

		expect(findConflictScrollRow(sparse, 50_000)?.index).toBe(1);
		expect(findConflictScrollRow(dense, 50_000)?.index).toBe(2);
		expect(findConflictScrollRow(dense, 49_000)?.index).toBe(1);
	});

	it("finds the visible measurement by its scroll offset", () => {
		const measurements = Array.from({ length: 100_000 }, (_, index) => ({
			index,
			start: index * 18,
			size: 18,
		}));
		expect(findMeasurementAtOffset(measurements, 900_005)?.index).toBe(50_000);
		expect(findMeasurementAtOffset(measurements, 1_800_000)?.index).toBe(
			99_999,
		);
	});
});

function line(baseLine: number): ConflictDiffItem {
	return {
		kind: "line",
		key: String(baseLine),
		baseLine,
		sourceLine: baseLine,
		text: "line",
		variant: "plain",
		syntaxSpans: [],
		changeSpans: [],
		hunkId: null,
		candidateIndex: null,
	};
}

function hunk(baseStartLine: number): ConflictDiffItem {
	return {
		kind: "hunk",
		hunk: {
			id: 0,
			baseStartLine,
			baseLineCount: 1,
			currentStartLine: baseStartLine,
			currentLineCount: 1,
			incomingStartLine: baseStartLine,
			incomingLineCount: 1,
		},
	};
}
