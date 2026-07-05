import { describe, expect, it } from "vitest";
import type { CommitFileDiffLine } from "@/generated/types";
import { isSameDiffLine } from "./WorkingTreeFileDiffHelpers";

describe("isSameDiffLine", () => {
	it("matches combined deleted and inserted rows for a synthetic modified action", () => {
		const action = line({
			changeType: "Modified",
			newLineNumber: 1,
			newText: "# Branch managers - LovelyGit visual refresh retest",
			oldLineNumber: 1,
			oldText: "# Branch managers",
		});

		expect(
			isSameDiffLine(
				line({
					changeType: "Deleted",
					oldLineNumber: 1,
					text: "# Branch managers",
				}),
				action,
			),
		).toBe(true);
		expect(
			isSameDiffLine(
				line({
					changeType: "Inserted",
					newLineNumber: 1,
					text: "# Branch managers - LovelyGit visual refresh retest",
				}),
				action,
			),
		).toBe(true);
	});
});

function line(line: Partial<CommitFileDiffLine>): CommitFileDiffLine {
	return {
		changeSpans: [],
		changeType: "",
		newChangeSpans: [],
		newLineNumber: null,
		newSyntaxSpans: [],
		newText: "",
		oldChangeSpans: [],
		oldLineNumber: null,
		oldSyntaxSpans: [],
		oldText: "",
		syntaxSpans: [],
		text: "",
		...line,
	};
}
