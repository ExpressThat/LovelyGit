import { describe, expect, it } from "vitest";
import { extractConflictLineRanges } from "./conflictLineRanges";

describe("extractConflictLineRanges", () => {
	it("materializes only requested lines and normalizes their endings", () => {
		const text = "zero\none\r\ntwo\nthree";
		const ranges = extractConflictLineRanges(
			text,
			[
				{ id: 2, startLine: 3, lineCount: 2 },
				{ id: 1, startLine: 1, lineCount: 1 },
				{ id: 3, startLine: 2, lineCount: 0 },
			],
			"\r\n",
		);

		expect(ranges.get(1)).toEqual(["zero\r\n"]);
		expect(ranges.get(2)).toEqual(["two\r\n", "three"]);
		expect(ranges.get(3)).toEqual([]);
	});

	it("supports overlapping ranges without duplicating unrelated lines", () => {
		const ranges = extractConflictLineRanges(
			"one\ntwo\nthree\nfour\n",
			[
				{ id: 1, startLine: 2, lineCount: 2 },
				{ id: 2, startLine: 3, lineCount: 2 },
			],
			"\n",
		);

		expect(ranges.get(1)).toEqual(["two\n", "three\n"]);
		expect(ranges.get(2)).toEqual(["three\n", "four\n"]);
	});

	it("scans a large source once while retaining only candidate lines", () => {
		const text = Array.from(
			{ length: 100_000 },
			(_, index) => `line ${index} stable content\n`,
		).join("");
		const startedAt = performance.now();

		const ranges = extractConflictLineRanges(
			text,
			[
				{ id: 1, startLine: 50_000, lineCount: 2 },
				{ id: 2, startLine: 90_000, lineCount: 1 },
			],
			"\n",
		);
		const elapsed = performance.now() - startedAt;

		expect(ranges.get(1)).toEqual([
			"line 49999 stable content\n",
			"line 50000 stable content\n",
		]);
		expect(ranges.get(2)).toEqual(["line 89999 stable content\n"]);
		expect(elapsed).toBeLessThan(250);
	});
});
