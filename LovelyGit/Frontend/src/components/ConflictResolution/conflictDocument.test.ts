import { describe, expect, it, vi } from "vitest";
import {
	areConflictChoicesResolved,
	createConflictChoices,
	createConflictDocument,
	hasConflictMarkers,
	parseConflictDocument,
	renderConflictResult,
} from "./conflictDocument";

const conflict = [
	"before\n",
	"<<<<<<< HEAD\n",
	"ours one\n",
	"ours two\n",
	"||||||| base\n",
	"base\n",
	"=======\n",
	"theirs one\n",
	">>>>>>> feature\n",
	"after\n",
].join("");

describe("conflictDocument", () => {
	it("parses common, current, base, and incoming sections", () => {
		const segments = parseConflictDocument(conflict);
		expect(segments).toHaveLength(3);
		expect(segments[1]).toMatchObject({
			kind: "conflict",
			ours: ["ours one\n", "ours two\n"],
			base: ["base\n"],
			theirs: ["theirs one\n"],
		});
	});

	it("starts marker-free then renders selected lines in order", () => {
		const segments = parseConflictDocument(conflict);
		const choices = createConflictChoices(segments);
		expect(renderConflictResult(segments, choices)).toBe(
			"before\nbase\nafter\n",
		);
		choices[0] = {
			resolution: "selection",
			ours: { accepted: true, lines: [true, false] },
			theirs: { accepted: true, lines: [true] },
		};
		expect(renderConflictResult(segments, choices)).toBe(
			"before\nours one\ntheirs one\nafter\n",
		);
	});

	it("requires a deliberate resolution even though the draft has no markers", () => {
		const segments = parseConflictDocument(conflict);
		const choices = createConflictChoices(segments);

		expect(hasConflictMarkers(renderConflictResult(segments, choices))).toBe(
			false,
		);
		expect(areConflictChoicesResolved(segments, choices)).toBe(false);
		choices[0] = { ...choices[0], resolution: "omit" };
		expect(areConflictChoicesResolved(segments, choices)).toBe(true);
	});

	it("keeps malformed markers editable and detects marker lines only", () => {
		const malformed = "<<<<<<< HEAD\nno separator\n";
		expect(renderConflictResult(parseConflictDocument(malformed), {})).toBe(
			malformed,
		);
		expect(hasConflictMarkers(malformed)).toBe(true);
		expect(hasConflictMarkers("const divider = '=======';")).toBe(false);
	});

	it("normalizes native candidate lines to the worktree newline style", () => {
		const document = createConflictDocument({
			base: { text: "before\nbase\nafter\n" },
			ours: { text: "before\ncurrent\nafter\n" },
			theirs: { text: "before\nincoming\nafter\n" },
			result: {
				text: "before\r\n<<<<<<< HEAD\r\ncurrent\r\n=======\r\nincoming\r\n>>>>>>> feature\r\nafter\r\n",
			},
			hunks: [
				{
					id: 0,
					baseStartLine: 2,
					baseLineCount: 1,
					currentStartLine: 2,
					currentLineCount: 1,
					incomingStartLine: 2,
					incomingLineCount: 1,
				},
			],
		} as never);
		const choices = createConflictChoices(document);
		choices[0] = {
			...choices[0],
			resolution: "selection",
			ours: { accepted: true, lines: [true] },
		};

		expect(renderConflictResult(document, choices)).toBe(
			"before\r\ncurrent\r\nafter\r\n",
		);
	});

	it("does not split complete source files when authoritative hunk ranges exist", () => {
		const match = vi.spyOn(String.prototype, "match");
		try {
			createConflictDocument({
				base: { text: "before\nbase\nafter\n" },
				ours: { text: "before\ncurrent\nafter\n" },
				theirs: { text: "before\nincoming\nafter\n" },
				result: { text: conflict },
				hunks: [
					{
						id: 0,
						baseStartLine: 2,
						baseLineCount: 1,
						currentStartLine: 2,
						currentLineCount: 1,
						incomingStartLine: 2,
						incomingLineCount: 1,
					},
				],
			} as never);

			expect(match).not.toHaveBeenCalled();
		} finally {
			match.mockRestore();
		}
	});
});
