import { describe, expect, it } from "vitest";
import { decodeDeltaReferenceLines, toDiffLine } from "./compactLinePayload";

describe("compactLinePayload", () => {
	it("restores syntax and intra-line change spans", () => {
		const line = toDiffLine([
			4,
			5,
			"old value",
			"new value",
			"",
			"Modified",
			[[0, 3, "keyword"]],
			[[4, 5, "string"]],
			[],
			[[0, 3, "Deleted"]],
			[[4, 5, "Inserted"]],
			[],
		]);

		expect(line.oldSyntaxSpans).toEqual([
			{ start: 0, length: 3, scope: "keyword" },
		]);
		expect(line.newSyntaxSpans).toEqual([
			{ start: 4, length: 5, scope: "string" },
		]);
		expect(line.oldChangeSpans).toEqual([
			{ start: 0, length: 3, changeType: "Deleted" },
		]);
		expect(line.newChangeSpans).toEqual([
			{ start: 4, length: 5, changeType: "Inserted" },
		]);
	});

	it("accepts legacy tuples without rendering spans", () => {
		const line = toDiffLine([1, 1, "", "", "text", "Unchanged"]);

		expect(line.text).toBe("text");
		expect(line.syntaxSpans).toEqual([]);
		expect(line.changeSpans).toEqual([]);
	});

	it("hydrates reference tuples from authoritative source lines", () => {
		const line = toDiffLine(
			[2, 3, null, null, null, "Modified", [], [], [], [], [], []],
			{
				oldLines: ["base one", "base two"],
				newLines: ["source one", "source two", "source three"],
			},
		);

		expect(line.oldText).toBe("base two");
		expect(line.newText).toBe("source three");
		expect(line.changeType).toBe("Modified");
	});

	it("decodes delta line identities and optional rendering spans", () => {
		const lines = decodeDeltaReferenceLines(
			[
				[1, 1, 0],
				[1, 1, 1, [[0, 4, "keyword"]], [], [], [], [[0, 6, "Inserted"]]],
				[1, null, 2],
				[null, 1, 3],
			],
			"base one\nbase two\nbase three",
			"source one\nsource two\nsource four",
		);

		expect(
			lines.map((line) => [line.oldLineNumber, line.newLineNumber]),
		).toEqual([
			[1, 1],
			[2, 2],
			[3, null],
			[null, 3],
		]);
		expect(lines[1].oldText).toBe("base two");
		expect(lines[1].newText).toBe("source two");
		expect(lines[1].oldSyntaxSpans[0].scope).toBe("keyword");
		expect(lines[1].newChangeSpans[0].changeType).toBe("Inserted");
	});
});
