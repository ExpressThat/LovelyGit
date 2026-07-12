import { describe, expect, it } from "vitest";
import { toDiffLine } from "./compactLinePayload";

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
});
