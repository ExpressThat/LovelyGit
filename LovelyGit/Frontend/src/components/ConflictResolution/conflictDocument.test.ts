import { describe, expect, it } from "vitest";
import {
	createConflictChoices,
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

	it("preserves unresolved blocks then renders selected lines in order", () => {
		const segments = parseConflictDocument(conflict);
		const choices = createConflictChoices(segments);
		expect(renderConflictResult(segments, choices)).toBe(conflict);
		choices[0] = {
			mode: "custom",
			ours: [true, false],
			theirs: [true],
		};
		expect(renderConflictResult(segments, choices)).toBe(
			"before\nours one\ntheirs one\nafter\n",
		);
	});

	it("keeps malformed markers editable and detects marker lines only", () => {
		const malformed = "<<<<<<< HEAD\nno separator\n";
		expect(renderConflictResult(parseConflictDocument(malformed), {})).toBe(
			malformed,
		);
		expect(hasConflictMarkers(malformed)).toBe(true);
		expect(hasConflictMarkers("const divider = '=======';")).toBe(false);
	});
});
