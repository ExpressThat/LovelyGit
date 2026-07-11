import type {
	CommitFileDiffLine,
	WorkingTreeChangedFile,
	WorkingTreePatchLine,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { workingNewText, workingOldText } from "./WorkingTreeFileDiffHelpers";

export function moveWorkingTreeLine(
	kind: "stage" | "unstage",
	repositoryId: string,
	file: WorkingTreeChangedFile,
	line: CommitFileDiffLine,
) {
	return sendRequestWithResponse({
		commandType:
			kind === "stage" ? "StageWorkingTreeLine" : "UnstageWorkingTreeLine",
		arguments: {
			...toPatchLine(line),
			group: file.group,
			path: file.path,
			repositoryId,
		},
	});
}

export function moveWorkingTreeHunk(
	kind: "stage" | "unstage",
	repositoryId: string,
	file: WorkingTreeChangedFile,
	lines: CommitFileDiffLine[],
) {
	return sendRequestWithResponse({
		commandType:
			kind === "stage" ? "StageWorkingTreeHunk" : "UnstageWorkingTreeHunk",
		arguments: {
			group: file.group,
			lines: lines.map(toPatchLine),
			path: file.path,
			repositoryId,
		},
	});
}

function toPatchLine(line: CommitFileDiffLine): WorkingTreePatchLine {
	return {
		changeType: line.changeType,
		newLineNumber: line.newLineNumber,
		newText: workingNewText(line),
		oldLineNumber: line.oldLineNumber,
		oldText: workingOldText(line),
	};
}
