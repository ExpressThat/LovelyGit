import { useEffect, useState } from "react";
import type {
	CommitFileDiffLine,
	CommitFileDiffResponse,
} from "@/generated/types";
import { loadReferencedCompactLines } from "../CommitFileDiff/compactLinePayload";
import { splitLines } from "./conflictDocument";

export function useConflictDiffLines(
	diff: CommitFileDiffResponse | null,
	baseText: string,
	sourceText: string,
) {
	const [state, setState] = useState<
		| { status: "loading" }
		| { status: "error"; message: string }
		| { status: "ready"; lines: CommitFileDiffLine[] }
	>(() => readyState(diff, sourceText));

	useEffect(() => {
		if (!diff?.compactLinesGzipBase64) {
			setState(readyState(diff, sourceText));
			return;
		}
		let active = true;
		setState({ status: "loading" });
		loadReferencedCompactLines(diff, baseText, sourceText)
			.then((lines) => {
				if (active) setState({ status: "ready", lines });
			})
			.catch((error: unknown) => {
				if (active) {
					setState({
						status: "error",
						message:
							error instanceof Error
								? error.message
								: "Failed to load comparison.",
					});
				}
			});
		return () => {
			active = false;
		};
	}, [baseText, diff, sourceText]);

	return state;
}

function readyState(diff: CommitFileDiffResponse | null, sourceText: string) {
	if (!diff) {
		return { status: "ready" as const, lines: fallbackLines(sourceText) };
	}
	if (diff.lines.length > 0 || !diff.virtualTextGzipBase64) {
		return { status: "ready" as const, lines: diff.lines };
	}
	return { status: "ready" as const, lines: fallbackLines(sourceText) };
}

function fallbackLines(text: string): CommitFileDiffLine[] {
	return splitLines(text).map((line, index) => ({
		oldLineNumber: index + 1,
		newLineNumber: index + 1,
		oldText: line.replace(/\r?\n$/, ""),
		newText: line.replace(/\r?\n$/, ""),
		text: line.replace(/\r?\n$/, ""),
		changeType: "Unchanged",
		oldSyntaxSpans: [],
		newSyntaxSpans: [],
		syntaxSpans: [],
		oldChangeSpans: [],
		newChangeSpans: [],
		changeSpans: [],
	}));
}
