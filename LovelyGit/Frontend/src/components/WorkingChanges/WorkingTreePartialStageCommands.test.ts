import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	CommitFileDiffLine,
	WorkingTreeChangedFile,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import {
	moveWorkingTreeHunk,
	moveWorkingTreeLine,
} from "./WorkingTreePartialStageCommands";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

describe("working tree partial-stage commands", () => {
	beforeEach(() => vi.clearAllMocks());

	it("sends an atomic typed hunk payload", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(undefined);
		const first = changedLine("Modified", 1, 1, "old", "new");
		const second = changedLine("Inserted", null, 4, "", "added");

		await moveWorkingTreeHunk("stage", "repo", file, [first, second]);

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: {
				group: "Unstaged",
				lines: [
					{
						changeType: "Modified",
						newLineNumber: 1,
						newText: "new",
						oldLineNumber: 1,
						oldText: "old",
					},
					{
						changeType: "Inserted",
						newLineNumber: 4,
						newText: "added",
						oldLineNumber: null,
						oldText: "",
					},
				],
				path: "file.txt",
				repositoryId: "repo",
			},
			commandType: "StageWorkingTreeHunk",
		});
	});

	it("uses the unstage command and preserves failures for retry", async () => {
		const error = new Error("patch no longer applies");
		vi.mocked(sendRequestWithResponse).mockRejectedValueOnce(error);

		await expect(
			moveWorkingTreeHunk("unstage", "repo", file, [
				changedLine("Deleted", 2, null, "gone", ""),
			]),
		).rejects.toBe(error);
		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			expect.objectContaining({ commandType: "UnstageWorkingTreeHunk" }),
		);
	});

	it("keeps individual-line staging on its focused command", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(undefined);

		await moveWorkingTreeLine(
			"stage",
			"repo",
			file,
			changedLine("Inserted", null, 2, "", "new"),
		);

		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			expect.objectContaining({ commandType: "StageWorkingTreeLine" }),
		);
	});
});

const file: WorkingTreeChangedFile = {
	additions: 2,
	deletions: 1,
	group: "Unstaged",
	isBinary: false,
	oldPath: null,
	path: "file.txt",
	status: "Modified",
};

function changedLine(
	changeType: string,
	oldLineNumber: number | null,
	newLineNumber: number | null,
	oldText: string,
	newText: string,
): CommitFileDiffLine {
	return {
		changeSpans: [],
		changeType,
		newChangeSpans: [],
		newLineNumber,
		newSyntaxSpans: [],
		newText,
		oldChangeSpans: [],
		oldLineNumber,
		oldSyntaxSpans: [],
		oldText,
		syntaxSpans: [],
		text: newText || oldText,
	};
}
