import type { CommitFileDiffLine } from "@/generated/types";
import { type DiffHunkAction, DiffHunkActionButton } from "./DiffHunkActions";
import {
	CodeCell,
	LineNumber,
	lineBackground,
	newSideVariant,
	oldSideVariant,
} from "./DiffLineRendering";
import { type DiffLineAction, DiffLineActionButton } from "./DiffRows";

export function DiffPaneHeader({
	hasAction = false,
	headerLabel,
	lineNumberLabel,
}: {
	hasAction?: boolean;
	headerLabel: string;
	lineNumberLabel: string;
}) {
	return (
		<div
			className={`grid ${hasAction ? "grid-cols-[4rem_minmax(0,1fr)_2rem]" : "grid-cols-[4rem_minmax(0,1fr)]"} border-r last:border-r-0`}
		>
			<div className="border-r px-2 py-1 text-right">{lineNumberLabel}</div>
			<div className="px-2 py-1">{headerLabel}</div>
			{hasAction ? (
				<div className="border-l px-1 py-1 text-center">Stage</div>
			) : null}
		</div>
	);
}

export function SideBySideRow({
	isLineActionBusy = false,
	hunkAction,
	line,
	lineAction,
	rowHeight,
	scrollLeft,
	side,
	wrapLines,
}: {
	isLineActionBusy?: boolean;
	hunkAction?: DiffHunkAction;
	line: CommitFileDiffLine;
	lineAction?: DiffLineAction;
	rowHeight?: number;
	scrollLeft: number;
	side: "old" | "new";
	wrapLines: boolean;
}) {
	const isOld = side === "old";
	return (
		<div
			className={`relative grid min-w-0 select-text ${lineAction ? "grid-cols-[4rem_minmax(0,1fr)_2rem]" : "grid-cols-[4rem_minmax(0,1fr)]"} ${lineBackground(line.changeType)}`}
			style={rowHeight === undefined ? undefined : { minHeight: rowHeight }}
		>
			<LineNumber value={isOld ? line.oldLineNumber : line.newLineNumber} />
			<CodeCell
				changeSpans={isOld ? line.oldChangeSpans : line.newChangeSpans}
				scrollLeft={scrollLeft}
				spans={isOld ? line.oldSyntaxSpans : line.newSyntaxSpans}
				text={isOld ? line.oldText : line.newText}
				variant={
					isOld
						? oldSideVariant(line.changeType)
						: newSideVariant(line.changeType)
				}
				wrapLines={wrapLines}
			/>
			{lineAction ? (
				<DiffLineActionButton
					action={lineAction}
					disabled={isLineActionBusy}
					line={line}
				/>
			) : null}
			{hunkAction ? (
				<DiffHunkActionButton action={hunkAction} disabled={isLineActionBusy} />
			) : null}
		</div>
	);
}
