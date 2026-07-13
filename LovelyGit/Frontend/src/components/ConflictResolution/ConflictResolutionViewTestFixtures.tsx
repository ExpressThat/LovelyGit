import { render } from "@testing-library/react";
import type {
	CommitFileDiffLine,
	CommitFileDiffResponse,
	ConflictFileVersion,
	ConflictResolutionResponse,
	WorkingTreeChangedFile,
} from "@/generated/types";
import { ConflictResolutionView } from "./ConflictResolutionView";

export function renderConflictView(onChange: () => void, onClose: () => void) {
	return render(
		<ConflictResolutionView
			file={conflictFile()}
			onChange={onChange}
			onClose={onClose}
			repositoryId="repo-1"
		/>,
	);
}

export function version(text: string): ConflictFileVersion {
	return {
		exists: true,
		isBinary: false,
		isTooLarge: false,
		sizeBytes: text.length,
		text,
		textGzipBase64: "",
		textEncoding: "",
	};
}

export function response(overrides?: {
	base?: string;
	current?: string;
	incoming?: string;
	result?: string;
}): ConflictResolutionResponse {
	const base = overrides?.base ?? "before\nbase\nafter\n";
	const current = overrides?.current ?? "before\ncurrent\nafter\n";
	const incoming = overrides?.incoming ?? "before\nincoming\nafter\n";
	const result =
		overrides?.result ??
		"before\n<<<<<<< HEAD\ncurrent\n||||||| base\nbase\n=======\nincoming\n>>>>>>> feature\nafter\n";
	const currentCount = candidateCount(current);
	const incomingCount = candidateCount(incoming);
	return {
		path: "src/file.txt",
		worktreeFingerprint: "ABC123",
		compactTextSchema: "",
		compactTextBundleGzipBase64: "",
		base: version(base),
		ours: version(current),
		theirs: version(incoming),
		result: version(result),
		currentSource: {
			label: "Current",
			refName: "main",
			objectId: "111111111111",
		},
		incomingSource: {
			label: "Incoming",
			refName: "feature",
			objectId: "222222222222",
		},
		hunks: [
			{
				id: 0,
				baseStartLine: 2,
				baseLineCount: base.includes("base") ? 1 : 0,
				currentStartLine: 2,
				currentLineCount: currentCount,
				incomingStartLine: 2,
				incomingLineCount: incomingCount,
			},
		],
		currentComparison: comparison(base, current),
		incomingComparison: comparison(base, incoming),
	};
}

function candidateCount(text: string) {
	return Math.max(0, text.split(/\r?\n/).filter(Boolean).length - 2);
}

function comparison(base: string, source: string): CommitFileDiffResponse {
	const baseLines = base.split(/\r?\n/).filter(Boolean);
	const sourceLines = source.split(/\r?\n/).filter(Boolean);
	const lines: CommitFileDiffLine[] = [plainLine(1, "before")];
	if (baseLines.length > 2) {
		if (sourceLines.length > 2) {
			lines.push(diffLine(2, 2, baseLines[1], sourceLines[1], "Modified"));
			for (let index = 2; index < sourceLines.length - 1; index++) {
				lines.push(
					diffLine(null, index + 1, "", sourceLines[index], "Inserted"),
				);
			}
		} else {
			lines.push(diffLine(2, null, baseLines[1], "", "Deleted"));
		}
	} else {
		for (let index = 1; index < sourceLines.length - 1; index++) {
			lines.push(diffLine(null, index + 1, "", sourceLines[index], "Inserted"));
		}
	}
	lines.push(plainLine(baseLines.length, "after", sourceLines.length));
	return {
		commitHash: "CONFLICT",
		path: "src/file.txt",
		status: "Unmerged",
		viewMode: "SideBySide",
		isBinary: false,
		hasDifferences: true,
		isTruncated: false,
		truncationMessage: "",
		virtualText: "",
		virtualTextGzipBase64: "",
		virtualTextEncoding: "",
		virtualChangeType: "",
		virtualLineCount: 0,
		compactLineSchema: "",
		compactLinesGzipBase64: "",
		compactLineCount: 0,
		compactSourceSchema: "",
		compactSourceBundleGzipBase64: "",
		lines,
	};
}

function plainLine(oldLine: number, text: string, newLine = oldLine) {
	return diffLine(oldLine, newLine, text, text, "Unchanged");
}

function diffLine(
	oldLineNumber: number | null,
	newLineNumber: number | null,
	oldText: string,
	newText: string,
	changeType: string,
): CommitFileDiffLine {
	return {
		oldLineNumber,
		newLineNumber,
		oldText,
		newText,
		text: newText || oldText,
		changeType,
		oldSyntaxSpans: [],
		newSyntaxSpans: [],
		syntaxSpans: [],
		oldChangeSpans: [],
		newChangeSpans: [],
		changeSpans: [],
	};
}

export function conflictFile(): WorkingTreeChangedFile {
	return {
		path: "src/file.txt",
		oldPath: null,
		status: "Unmerged",
		group: "Unmerged",
		additions: 0,
		deletions: 0,
		isBinary: false,
	};
}
