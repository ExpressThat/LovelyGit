import { useVirtualizer } from "@tanstack/react-virtual";
import { useMemo, useRef, useState } from "react";
import type { CommitFileDiffLine } from "@/generated/types";
import { DiffHorizontalScroller } from "./DiffHorizontalScroller";
import { DiffHunkActionButton, getDiffHunkAction } from "./DiffHunkActions";
import {
	CodeCell,
	changeMarker,
	LineNumber,
	lineBackground,
} from "./DiffLineRendering";
import { estimateCodeWidth } from "./DiffLineViewport";
import {
	DiffChunkSeparator,
	type DiffDisplayRow,
	DiffLineActionButton,
	getCombinedLineAction,
	getCombinedLineActionPayload,
} from "./DiffRows";

const DIFF_OVERSCAN = 12;
const EMPTY_HUNK_LOOKUP = new Map<CommitFileDiffLine, CommitFileDiffLine[]>();

export function CombinedDiff({
	isLineActionBusy = false,
	hunkLookup = EMPTY_HUNK_LOOKUP,
	lines,
	onStageLine,
	onStageHunk,
	onUnstageLine,
	onUnstageHunk,
	wrapLines,
}: {
	isLineActionBusy?: boolean;
	hunkLookup?: Map<CommitFileDiffLine, CommitFileDiffLine[]>;
	lines: DiffDisplayRow[];
	onStageLine?: (line: CommitFileDiffLine) => void;
	onStageHunk?: (lines: CommitFileDiffLine[]) => void;
	onUnstageLine?: (line: CommitFileDiffLine) => void;
	onUnstageHunk?: (lines: CommitFileDiffLine[]) => void;
	wrapLines: boolean;
}) {
	const hasLineAction = Boolean(onStageLine || onUnstageLine);
	const viewportRef = useRef<HTMLDivElement | null>(null);
	const [scrollLeft, setScrollLeft] = useState(0);
	const contentWidth = useMemo(
		() => estimateCodeWidth(iterCombinedText(lines)),
		[lines],
	);
	const virtualizer = useVirtualizer({
		count: lines.length,
		estimateSize: () => 18,
		getScrollElement: () => viewportRef.current,
		measureElement: (element) => element.getBoundingClientRect().height,
		overscan: DIFF_OVERSCAN,
	});
	const virtualItems = virtualizer.getVirtualItems();

	return (
		<div className="flex h-full min-w-0 flex-col font-mono text-[12px] leading-[18px] text-foreground">
			<div
				className={`grid shrink-0 ${hasLineAction ? "grid-cols-[4rem_4rem_2rem_minmax(0,1fr)_2rem]" : "grid-cols-[4rem_4rem_2rem_minmax(0,1fr)]"} border-b bg-card text-[10px] font-semibold uppercase text-muted-foreground`}
			>
				<div className="border-r px-2 py-1 text-right">Old</div>
				<div className="border-r px-2 py-1 text-right">New</div>
				<div className="border-r px-2 py-1 text-center"> </div>
				<div className="px-2 py-1">Code</div>
				{hasLineAction ? (
					<div className="border-l px-1 py-1 text-center">Index</div>
				) : null}
			</div>
			<div
				className="custom-scrollbar relative min-h-0 flex-1 overflow-x-hidden overflow-y-auto"
				ref={viewportRef}
			>
				<div
					className="relative w-full"
					style={{ height: `${virtualizer.getTotalSize()}px` }}
				>
					{virtualItems.map((item) => {
						const row = lines[item.index];
						if (row.kind === "separator") {
							return (
								<div
									className="absolute left-0 top-0 w-full"
									data-index={item.index}
									key={item.key}
									ref={virtualizer.measureElement}
									style={{ transform: `translateY(${item.start}px)` }}
								>
									<DiffChunkSeparator />
								</div>
							);
						}

						const line = row.line;
						const actionPayload = getCombinedLineActionPayload(
							lines,
							item.index,
						);
						const lineAction = getCombinedLineAction(
							actionPayload ?? line,
							onStageLine,
							onUnstageLine,
						);
						const hunkAction = getDiffHunkAction(
							line,
							hunkLookup,
							onStageHunk,
							onUnstageHunk,
						);
						return (
							<div
								className={`absolute left-0 top-0 grid w-full ${hasLineAction ? "grid-cols-[4rem_4rem_2rem_minmax(0,1fr)_2rem]" : "grid-cols-[4rem_4rem_2rem_minmax(0,1fr)]"} ${lineBackground(line.changeType)}`}
								data-index={item.index}
								key={item.key}
								ref={virtualizer.measureElement}
								style={{ transform: `translateY(${item.start}px)` }}
							>
								<LineNumber value={line.oldLineNumber} />
								<LineNumber value={line.newLineNumber} />
								<div className="border-r px-2 text-center text-muted-foreground">
									{changeMarker(line.changeType)}
								</div>
								<CodeCell
									changeSpans={line.changeSpans}
									scrollLeft={scrollLeft}
									spans={line.syntaxSpans}
									text={line.text}
									variant={
										line.changeType === "Deleted"
											? "deleted"
											: line.changeType === "Inserted"
												? "inserted"
												: "plain"
									}
									wrapLines={wrapLines}
								/>
								{lineAction ? (
									<DiffLineActionButton
										action={lineAction}
										disabled={isLineActionBusy}
										line={actionPayload ?? line}
									/>
								) : hasLineAction ? (
									<div className="border-l" />
								) : null}
								{hunkAction ? (
									<DiffHunkActionButton
										action={hunkAction}
										disabled={isLineActionBusy}
									/>
								) : null}
							</div>
						);
					})}
				</div>
			</div>
			{wrapLines ? null : (
				<DiffHorizontalScroller
					contentWidth={contentWidth}
					label="Horizontal combined diff scroll"
					onValueChange={setScrollLeft}
					value={scrollLeft}
				/>
			)}
		</div>
	);
}

function* iterCombinedText(lines: DiffDisplayRow[]) {
	for (const row of lines) {
		if (row.kind === "line") {
			yield row.line.text;
		}
	}
}
