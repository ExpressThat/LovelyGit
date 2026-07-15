import { describe, expect, it } from "vitest";
import type { ConflictHunk } from "@/generated/types";
import { createConflictHunkLookup } from "./conflictHunkLookup";

describe("createConflictHunkLookup", () => {
	it("preserves original hunk precedence across overlapping coordinates", () => {
		const first = hunk(0, 10, 100, 200, 100);
		const nested = hunk(1, 50, 10, 250, 10);
		const lookup = createConflictHunkLookup([first, nested], "ours");

		expect(lookup.find(55, null)?.id).toBe(0);
		expect(lookup.find(75, null)?.id).toBe(0);
		expect(lookup.find(null, 255)?.id).toBe(0);
	});

	it("chooses the earliest input hunk when base and source match differently", () => {
		const sourceMatch = hunk(0, 100, 1, 20, 1);
		const baseMatch = hunk(1, 10, 1, 200, 1);
		const lookup = createConflictHunkLookup([sourceMatch, baseMatch], "ours");

		expect(lookup.find(10, 20)?.id).toBe(0);
	});

	it("ignores empty candidates while retaining the opposite range", () => {
		const deletion = hunk(0, 10, 2, 10, 0);
		const lookup = createConflictHunkLookup([deletion], "ours");

		expect(lookup.find(10, null)?.id).toBe(0);
		expect(lookup.find(null, 10)).toBeUndefined();
	});
});

function hunk(
	id: number,
	baseStartLine: number,
	baseLineCount: number,
	currentStartLine: number,
	currentLineCount: number,
): ConflictHunk {
	return {
		id,
		baseStartLine,
		baseLineCount,
		currentStartLine,
		currentLineCount,
		incomingStartLine: currentStartLine,
		incomingLineCount: currentLineCount,
	};
}
