import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	CommitFileDiffLine,
	CommitFileDiffResponse,
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

		await moveWorkingTreeHunk("stage", "repo", file, diff, [first, second]);

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: {
				group: "Unstaged",
				lines: [
					{
						changeType: "Modified",
						newLineNumber: 1,
						newText: "new",
						newLineEnding: "\r\n",
						oldLineNumber: 1,
						oldText: "old",
						oldLineEnding: "\r\n",
					},
					{
						changeType: "Inserted",
						newLineNumber: 4,
						newText: "added",
						newLineEnding: "",
						oldLineNumber: null,
						oldText: "",
						oldLineEnding: null,
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
			moveWorkingTreeHunk("unstage", "repo", file, diff, [
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
			diff,
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

const diff = {
	newLineEnding: "\r\n",
	newLineEndingOverrides: [4 * 4 + 3],
	oldLineEnding: "\r\n",
	oldLineEndingOverrides: [],
} as unknown as CommitFileDiffResponse;

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
