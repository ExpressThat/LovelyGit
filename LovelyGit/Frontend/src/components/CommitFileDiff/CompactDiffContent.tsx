import { useEffect, useState } from "react";
import type {
	CommitFileDiffLine,
	CommitFileDiffResponse,
} from "@/generated/types";
import { CombinedDiff } from "./CombinedDiff";
import { loadCompactLines } from "./compactLinePayload";
import { buildDiffHunkLookupIfActionable } from "./DiffHunkActions";
import { type DiffDisplayRow, getContextualDiffRows } from "./DiffRows";
import { SideBySideDiff } from "./SideBySideDiff";

const COMPACT_LOADING_ROWS = Array.from({ length: 12 }, (_, index) => ({
	id: `compact-diff-loading-${index}`,
	width: index % 3 === 0 ? 72 : 96,
}));

export function CompactDiffContent({
	contextLines,
	diff,
	isLineActionBusy,
	lineDisplayMode,
	onStageLine,
	onStageHunk,
	onUnstageLine,
	onUnstageHunk,
	wrapLines,
}: {
	contextLines: number;
	diff: CommitFileDiffResponse;
	isLineActionBusy: boolean;
	lineDisplayMode: "Changes" | "FullFile";
	onStageLine?: (line: CommitFileDiffLine) => void;
	onStageHunk?: (lines: CommitFileDiffLine[]) => void;
	onUnstageLine?: (line: CommitFileDiffLine) => void;
	onUnstageHunk?: (lines: CommitFileDiffLine[]) => void;
	wrapLines: boolean;
}) {
	const [lines, setLines] = useState<CommitFileDiffLine[] | null>(null);
	const [error, setError] = useState<string | null>(null);
	useEffect(() => {
		let active = true;
		setLines(null);
		setError(null);
		loadCompactLines(diff)
			.then((loadedLines) => {
				if (active) {
					setLines(loadedLines);
				}
			})
			.catch((loadError: unknown) => {
				if (active) {
					setError(
						loadError instanceof Error
							? loadError.message
							: "Failed to load compact diff.",
					);
				}
			});
		return () => {
			active = false;
		};
	}, [diff]);

	if (error) {
		return <div className="m-4 rounded-md border p-3 text-sm">{error}</div>;
	}

	if (!lines) {
		return <CompactDiffLoading />;
	}

	if (lines.length === 0 || !lines.some(isChangedLine)) {
		return (
			<div className="m-4 rounded-md border bg-card p-4 text-sm text-muted-foreground">
				No textual differences.
			</div>
		);
	}

	const removeLine = (line: CommitFileDiffLine) => {
		setLines(
			(current) =>
				current?.filter(
					(currentLine) => !isLineActionTarget(currentLine, line),
				) ?? current,
		);
	};
	const stageLine = onStageLine
		? (line: CommitFileDiffLine) => {
				removeLine(line);
				onStageLine(line);
			}
		: undefined;
	const unstageLine = onUnstageLine
		? (line: CommitFileDiffLine) => {
				removeLine(line);
				onUnstageLine(line);
			}
		: undefined;
	const runHunk = (
		action: ((lines: CommitFileDiffLine[]) => void) | undefined,
		hunkLines: CommitFileDiffLine[],
	) => {
		if (!action) return;
		setLines((current) =>
			current
				? current.filter((line) =>
						hunkLines.every((target) => !isLineActionTarget(line, target)),
					)
				: null,
		);
		action(hunkLines);
	};
	const rows =
		lineDisplayMode === "FullFile"
			? lines.map((line): DiffDisplayRow => ({ kind: "line", line }))
			: getContextualDiffRows(lines, contextLines);
	const hunkLookup = buildDiffHunkLookupIfActionable(
		lines,
		contextLines,
		Boolean(onStageHunk || onUnstageHunk),
	);

	return diff.viewMode === "SideBySide" ? (
		<SideBySideDiff
			hunkLookup={hunkLookup}
			{...{
				isLineActionBusy,
				onStageLine: stageLine,
				onStageHunk: onStageHunk
					? (hunkLines) => runHunk(onStageHunk, hunkLines)
					: undefined,
				onUnstageLine: unstageLine,
				onUnstageHunk: onUnstageHunk
					? (hunkLines) => runHunk(onUnstageHunk, hunkLines)
					: undefined,
				wrapLines,
			}}
			lines={rows}
		/>
	) : (
		<CombinedDiff
			hunkLookup={hunkLookup}
			{...{
				isLineActionBusy,
				onStageLine: stageLine,
				onStageHunk: onStageHunk
					? (hunkLines) => runHunk(onStageHunk, hunkLines)
					: undefined,
				onUnstageLine: unstageLine,
				onUnstageHunk: onUnstageHunk
					? (hunkLines) => runHunk(onUnstageHunk, hunkLines)
					: undefined,
				wrapLines,
			}}
			lines={rows}
		/>
	);
}

function isLineActionTarget(
	line: CommitFileDiffLine,
	actionLine: CommitFileDiffLine,
) {
	if (
		actionLine.changeType === "Modified" &&
		(line.changeType === "Deleted" || line.changeType === "Inserted")
	) {
		return (
			(line.changeType === "Deleted" &&
				line.oldLineNumber === actionLine.oldLineNumber &&
				lineText(line) === oldLineText(actionLine)) ||
			(line.changeType === "Inserted" &&
				line.newLineNumber === actionLine.newLineNumber &&
				lineText(line) === newLineText(actionLine))
		);
	}

	return (
		line.changeType === actionLine.changeType &&
		line.oldLineNumber === actionLine.oldLineNumber &&
		line.newLineNumber === actionLine.newLineNumber &&
		line.oldText === actionLine.oldText &&
		line.newText === actionLine.newText &&
		line.text === actionLine.text
	);
}

function lineText(line: CommitFileDiffLine) {
	return line.text || line.oldText || line.newText;
}

function oldLineText(line: CommitFileDiffLine) {
	return line.oldText || line.text;
}

function newLineText(line: CommitFileDiffLine) {
	return line.newText || line.text;
}

function isChangedLine(line: CommitFileDiffLine) {
	return (
		line.changeType === "Added" ||
		line.changeType === "Deleted" ||
		line.changeType === "Inserted" ||
		line.changeType === "Modified"
	);
}

function CompactDiffLoading() {
	return (
		<div className="space-y-2 p-4">
			{COMPACT_LOADING_ROWS.map((row) => (
				<div
					className="h-5 animate-pulse rounded bg-muted"
					key={row.id}
					style={{ width: `${row.width}%` }}
				/>
			))}
		</div>
	);
}
