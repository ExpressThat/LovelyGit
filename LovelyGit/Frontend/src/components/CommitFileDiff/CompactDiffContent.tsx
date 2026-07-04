import { useEffect, useState } from "react";
import type {
	CommitFileDiffLine,
	CommitFileDiffResponse,
} from "@/generated/types";
import { CombinedDiff } from "./CombinedDiff";
import { loadCompactLines } from "./compactLinePayload";
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
	onUnstageLine,
	wrapLines,
}: {
	contextLines: number;
	diff: CommitFileDiffResponse;
	isLineActionBusy: boolean;
	lineDisplayMode: "Changes" | "FullFile";
	onStageLine?: (line: CommitFileDiffLine) => void;
	onUnstageLine?: (line: CommitFileDiffLine) => void;
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

	const rows =
		lineDisplayMode === "FullFile"
			? lines.map((line): DiffDisplayRow => ({ kind: "line", line }))
			: getContextualDiffRows(lines, contextLines);

	return diff.viewMode === "SideBySide" ? (
		<SideBySideDiff
			{...{ isLineActionBusy, onStageLine, onUnstageLine, wrapLines }}
			lines={rows}
		/>
	) : (
		<CombinedDiff
			{...{ isLineActionBusy, onStageLine, onUnstageLine, wrapLines }}
			lines={rows}
		/>
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
