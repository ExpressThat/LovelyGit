import { describe, expect, it } from "vitest";
import type { FileBlameHunk } from "@/generated/types";
import {
	buildBlameLineStarts,
	findBlameHunk,
	readBlameLine,
} from "./fileBlameLines";

describe("fileBlameLines", () => {
	it.each([
		["one\r\ntwo\n", 2, ["one", "two"]],
		["one\ntwo", 2, ["one", "two"]],
		["one\n\n", 2, ["one", ""]],
		["", 0, []],
	] as const)("indexes lines without retaining a string per row", (content, count, expected) => {
		const starts = buildBlameLineStarts(content, count);

		expect(
			Array.from({ length: count }, (_, index) =>
				readBlameLine(content, starts, index),
			),
		).toEqual(expected);
		expect(starts).toBeInstanceOf(Uint32Array);
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
