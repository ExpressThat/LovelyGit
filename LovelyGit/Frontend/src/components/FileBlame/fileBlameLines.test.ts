import { describe, expect, it } from "vitest";
import type { FileBlameHunk } from "@/generated/types";
import { findBlameHunk, splitBlameLines } from "./fileBlameLines";

describe("fileBlameLines", () => {
	it("splits content without inventing a trailing line", () => {
		expect(splitBlameLines("one\r\ntwo\n")).toEqual(["one", "two"]);
		expect(splitBlameLines("one\ntwo")).toEqual(["one", "two"]);
		expect(splitBlameLines("")).toEqual([]);
	});

	it("finds compact attribution hunks by line number", () => {
		const hunks = [hunk(1, 2, "one"), hunk(3, 4, "two")];

		expect(findBlameHunk(hunks, 1)?.hash).toBe("one");
		expect(findBlameHunk(hunks, 6)?.hash).toBe("two");
		expect(findBlameHunk(hunks, 7)).toBeNull();
	});
});

function hunk(
	startLine: number,
	lineCount: number,
	hash: string,
): FileBlameHunk {
	return {
		author: "Ross",
		date: 1,
		email: "ross@example.invalid",
		hash,
		lineCount,
		startLine,
		subject: "Change",
	};
}
