import { describe, expect, it } from "vitest";
import {
	applyConflictSide,
	composeConflictResult,
	composeConflictResultLines,
	parseConflictHunks,
} from "./ConflictHunks";

describe("conflict hunk helpers", () => {
	it("finds conflict blocks and replaces one side", () => {
		const text = [
			"a",
			"<<<<<<< HEAD",
			"current",
			"=======",
			"incoming",
			">>>>>>> branch",
			"z",
		].join("\n");
		const [hunk] = parseConflictHunks(text);

		expect(hunk).toMatchObject({
			current: ["current"],
			id: "2:6",
			incoming: ["incoming"],
			startLine: 2,
		});
		expect(applyConflictSide(text, hunk, "incoming")).toBe("a\nincoming\nz");
		expect(composeConflictResult(text, [hunk], ["current"])).toBe(
			"a\ncurrent\nz",
		);
		expect(
			composeConflictResultLines(
				lines(text),
				lines("a\ncurrent\nz"),
				lines("a\nincoming\nz"),
				[hunk],
				["incoming"],
			).map((line) => line.text),
		).toEqual(["a", "incoming", "z"]);
	});
});

function lines(text: string) {
	return text.split("\n").map((line, index) => ({
		lineNumber: index + 1,
		markerKind: "",
		syntaxSpans: [],
		text: line,
	}));
}
