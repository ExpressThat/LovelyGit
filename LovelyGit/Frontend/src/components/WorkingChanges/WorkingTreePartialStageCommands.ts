import type {
	CommitFileDiffLine,
	CommitFileDiffResponse,
	WorkingTreeChangedFile,
	WorkingTreePatchLine,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { workingNewText, workingOldText } from "./WorkingTreeFileDiffHelpers";

export function moveWorkingTreeLine(
	kind: "stage" | "unstage",
	repositoryId: string,
	file: WorkingTreeChangedFile,
	diff: CommitFileDiffResponse,
	line: CommitFileDiffLine,
) {
	return sendRequestWithResponse({
		commandType:
			kind === "stage" ? "StageWorkingTreeLine" : "UnstageWorkingTreeLine",
		arguments: {
			...toPatchLine(diff, line),
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
	diff: CommitFileDiffResponse,
	lines: CommitFileDiffLine[],
) {
	return sendRequestWithResponse({
		commandType:
			kind === "stage" ? "StageWorkingTreeHunk" : "UnstageWorkingTreeHunk",
		arguments: {
			group: file.group,
			lines: lines.map((line) => toPatchLine(diff, line)),
			path: file.path,
			repositoryId,
		},
	});
}

function toPatchLine(
	diff: CommitFileDiffResponse,
	line: CommitFileDiffLine,
): WorkingTreePatchLine {
	return {
		changeType: line.changeType,
		newLineEnding: resolveLineEnding(
			diff.newLineEnding,
			diff.newLineEndingOverrides,
			line.newLineNumber,
		),
		newLineNumber: line.newLineNumber,
		newText: workingNewText(line),
		oldLineEnding: resolveLineEnding(
			diff.oldLineEnding,
			diff.oldLineEndingOverrides,
			line.oldLineNumber,
		),
		oldLineNumber: line.oldLineNumber,
		oldText: workingOldText(line),
	};
}

function resolveLineEnding(
	fallback: string | null,
	overrides: number[],
	lineNumber: number | null,
) {
	if (lineNumber === null) return null;
	let low = 0;
	let high = overrides.length - 1;
	while (low <= high) {
		const middle = (low + high) >>> 1;
		const encoded = overrides[middle];
		const overrideLine = Math.floor(encoded / 4);
		if (overrideLine === lineNumber) return endingFromKind(encoded % 4);
		if (overrideLine < lineNumber) low = middle + 1;
		else high = middle - 1;
	}
	return fallback;
}

function endingFromKind(kind: number) {
	if (kind === 0) return "\r\n";
	if (kind === 1) return "\n";
	if (kind === 2) return "\r";
	return "";
}
