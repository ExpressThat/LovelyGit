import type {
	CommitFileDiffLine,
	CommitFileDiffResponse,
} from "@/generated/types";
import { CombinedDiff } from "./CombinedDiff";
import { CompactDiffContent } from "./CompactDiffContent";
import { hasCompactLinePayload } from "./compactLinePayload";
import { buildDiffHunkLookupIfActionable } from "./DiffHunkActions";
import { type DiffDisplayRow, getContextualDiffRows } from "./DiffRows";
import { SideBySideDiff } from "./SideBySideDiff";
import { VirtualTextDiff } from "./VirtualTextDiff";
import { hasVirtualTextPayload } from "./virtualTextPayload";

const LOADING_DIFF_ROWS = Array.from({ length: 16 }, (_, index) => ({
	id: `loading-diff-row-${index}`,
	width: index % 3 === 0 ? 72 : 96,
}));

export function LoadingDiff() {
	return (
		<div className="space-y-2 p-4">
			{LOADING_DIFF_ROWS.map((row) => (
				<div
					className="h-5 animate-pulse rounded bg-muted"
					key={row.id}
					style={{ width: `${row.width}%` }}
				/>
			))}
		</div>
	);
}

export function DiffContent({
	diff,
	contextLines,
	isLineActionBusy = false,
	lineDisplayMode,
	onStageLine,
	onStageHunk,
	onUnstageLine,
	onUnstageHunk,
	wrapLines,
}: {
	contextLines: number;
	diff: CommitFileDiffResponse;
	isLineActionBusy?: boolean;
	lineDisplayMode: "Changes" | "FullFile";
	onStageLine?: (line: CommitFileDiffLine) => void;
	onStageHunk?: (lines: CommitFileDiffLine[]) => void;
	onUnstageLine?: (line: CommitFileDiffLine) => void;
	onUnstageHunk?: (lines: CommitFileDiffLine[]) => void;
	wrapLines: boolean;
}) {
	if (diff.isTruncated) {
		return (
			<div className="m-4 rounded-md border bg-card p-4 text-sm text-muted-foreground">
				{diff.truncationMessage ||
					"Diff skipped because the file is too large."}
			</div>
		);
	}

	if (diff.isBinary) {
		return (
			<div className="m-4 rounded-md border bg-card p-4 text-sm text-muted-foreground">
				Binary file diff is not available.
			</div>
		);
	}

	if (hasVirtualTextPayload(diff)) {
		return <VirtualTextDiff diff={diff} wrapLines={wrapLines} />;
	}

	if (hasCompactLinePayload(diff)) {
		return (
			<CompactDiffContent
				{...{
					contextLines,
					diff,
					isLineActionBusy,
					lineDisplayMode,
					onStageLine,
					onStageHunk,
					onUnstageLine,
					onUnstageHunk,
					wrapLines,
				}}
			/>
		);
	}

	if (diff.lines.length === 0 || !diff.hasDifferences) {
		return (
			<div className="m-4 rounded-md border bg-card p-4 text-sm text-muted-foreground">
				No textual differences.
			</div>
		);
	}

	const lines =
		lineDisplayMode === "FullFile"
			? diff.lines.map((line): DiffDisplayRow => ({ kind: "line", line }))
			: getContextualDiffRows(diff.lines, contextLines);
	const hunkLookup = buildDiffHunkLookupIfActionable(
		diff.lines,
		contextLines,
		Boolean(onStageHunk || onUnstageHunk),
	);

	return diff.viewMode === "SideBySide" ? (
		<SideBySideDiff
			hunkLookup={hunkLookup}
			isLineActionBusy={isLineActionBusy}
			lines={lines}
			onStageLine={onStageLine}
			onStageHunk={onStageHunk}
			onUnstageLine={onUnstageLine}
			onUnstageHunk={onUnstageHunk}
			wrapLines={wrapLines}
		/>
	) : (
		<CombinedDiff
			hunkLookup={hunkLookup}
			isLineActionBusy={isLineActionBusy}
			lines={lines}
			onStageLine={onStageLine}
			onStageHunk={onStageHunk}
			onUnstageLine={onUnstageLine}
			onUnstageHunk={onUnstageHunk}
			wrapLines={wrapLines}
		/>
	);
}
