import { useVirtualizer } from "@tanstack/react-virtual";
import { useMemo, useRef } from "react";
import type { CommitFileDiffLine } from "@/generated/types";
import { getSideDiffHunkAction } from "./DiffHunkActions";
import { estimateCodeWidth } from "./DiffLineRendering";
import {
	DiffChunkSeparator,
	type DiffDisplayRow,
	getSideBySideLineAction,
} from "./DiffRows";
import { DiffPaneHeader, SideBySideRow } from "./SideBySideRow";
import { useSynchronizedDiffScroll } from "./useSynchronizedDiffScroll";

const DIFF_OVERSCAN = 12;
const EMPTY_HUNK_LOOKUP = new Map<CommitFileDiffLine, CommitFileDiffLine[]>();

export function SideBySideDiff({
	isLineActionBusy = false,
	lines,
	hunkLookup = EMPTY_HUNK_LOOKUP,
	onStageLine,
	onStageHunk,
	onUnstageLine,
	onUnstageHunk,
	wrapLines,
}: {
	isLineActionBusy?: boolean;
	lines: DiffDisplayRow[];
	hunkLookup?: Map<CommitFileDiffLine, CommitFileDiffLine[]>;
	onStageHunk?: (lines: CommitFileDiffLine[]) => void;
	onStageLine?: (line: CommitFileDiffLine) => void;
	onUnstageHunk?: (lines: CommitFileDiffLine[]) => void;
	onUnstageLine?: (line: CommitFileDiffLine) => void;
	wrapLines: boolean;
}) {
	const hasLineAction = Boolean(onStageLine || onUnstageLine);
	const viewportRef = useRef<HTMLDivElement | null>(null);
	const scroll = useSynchronizedDiffScroll();
	const contentWidth = useMemo(
		() => estimateCodeWidth(iterSideBySideText(lines)),
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
			<div className="grid shrink-0 grid-cols-2 border-b bg-card text-[10px] font-semibold uppercase text-muted-foreground">
				<DiffPaneHeader
					hasAction={hasLineAction}
					headerLabel="Before"
					lineNumberLabel="Old"
				/>
				<DiffPaneHeader
					hasAction={hasLineAction}
					headerLabel="After"
					lineNumberLabel="New"
				/>
			</div>
			<div
				className="custom-scrollbar relative min-h-0 flex-1 overflow-x-hidden overflow-y-auto"
				ref={viewportRef}
			>
				<div
					className="relative w-full"
					style={{ height: `${virtualizer.getTotalSize()}px` }}
				>
					<div aria-hidden="true" className="pointer-events-none invisible">
						{virtualItems.map((item) => {
							const line = lines[item.index];
							if (line.kind === "separator") {
								return (
									<div
										className="absolute left-0 top-0 w-full"
										data-index={item.index}
										key={`measure:${item.key}`}
										ref={virtualizer.measureElement}
										style={{ transform: `translateY(${item.start}px)` }}
									>
										<DiffChunkSeparator />
									</div>
								);
							}
							return (
								<div
									className="absolute left-0 top-0 grid w-full grid-cols-2"
									data-index={item.index}
									key={`measure:${item.key}`}
									ref={virtualizer.measureElement}
									style={{ transform: `translateY(${item.start}px)` }}
								>
									<SideBySideRow
										line={line.line}
										lineAction={undefined}
										scrollLeft={0}
										side="old"
										width={contentWidth}
										wrapLines={wrapLines}
									/>
									<SideBySideRow
										line={line.line}
										lineAction={undefined}
										scrollLeft={0}
										side="new"
										width={contentWidth}
										wrapLines={wrapLines}
									/>
								</div>
							);
						})}
					</div>
					<div className="absolute inset-0 grid grid-cols-2">
						<div className="relative min-w-0 border-r">
							{virtualItems.map((item) => {
								const line = lines[item.index];
								if (line.kind === "separator") {
									return (
										<div
											className="absolute left-0 top-0 w-full"
											key={`old-separator:${item.key}`}
											style={{ transform: `translateY(${item.start}px)` }}
										>
											<DiffChunkSeparator />
										</div>
									);
								}
								return (
									<div
										className="absolute left-0 top-0 w-full"
										key={`old:${item.key}`}
										style={{ transform: `translateY(${item.start}px)` }}
									>
										<SideBySideRow
											hunkAction={getSideDiffHunkAction(
												line.line,
												"old",
												hunkLookup,
												onStageHunk,
												onUnstageHunk,
											)}
											line={line.line}
											isLineActionBusy={isLineActionBusy}
											lineAction={getSideBySideLineAction(
												line.line,
												"old",
												onStageLine,
												onUnstageLine,
											)}
											rowHeight={item.size}
											scrollLeft={scroll.oldScrollLeft}
											side="old"
											width={contentWidth}
											wrapLines={wrapLines}
										/>
									</div>
								);
							})}
						</div>
						<div className="relative min-w-0">
							{virtualItems.map((item) => {
								const line = lines[item.index];
								if (line.kind === "separator") {
									return (
										<div
											className="absolute left-0 top-0 w-full"
											key={`new-separator:${item.key}`}
											style={{ transform: `translateY(${item.start}px)` }}
										>
											<DiffChunkSeparator />
										</div>
									);
								}
								return (
									<div
										className="absolute left-0 top-0 w-full"
										key={`new:${item.key}`}
										style={{ transform: `translateY(${item.start}px)` }}
									>
										<SideBySideRow
											hunkAction={getSideDiffHunkAction(
												line.line,
												"new",
												hunkLookup,
												onStageHunk,
												onUnstageHunk,
											)}
											line={line.line}
											isLineActionBusy={isLineActionBusy}
											lineAction={getSideBySideLineAction(
												line.line,
												"new",
												onStageLine,
												onUnstageLine,
											)}
											rowHeight={item.size}
											scrollLeft={scroll.newScrollLeft}
											side="new"
											width={contentWidth}
											wrapLines={wrapLines}
										/>
									</div>
								);
							})}
						</div>
					</div>
				</div>
			</div>
			{wrapLines ? null : (
				<div className="grid h-3 shrink-0 grid-cols-2 border-t bg-background">
					<div
						className="custom-scrollbar overflow-x-auto overflow-y-hidden border-r"
						onScroll={(event) => scroll.syncBottomScroll("old", event)}
						ref={scroll.oldScrollerRef}
					>
						<div style={{ height: 1, width: contentWidth }} />
					</div>
					<div
						className="custom-scrollbar overflow-x-auto overflow-y-hidden"
						onScroll={(event) => scroll.syncBottomScroll("new", event)}
						ref={scroll.newScrollerRef}
					>
						<div style={{ height: 1, width: contentWidth }} />
					</div>
				</div>
			)}
		</div>
	);
}

function* iterSideBySideText(lines: DiffDisplayRow[]) {
	for (const row of lines) {
		if (row.kind !== "line") {
			continue;
		}

		yield row.line.oldText;
		yield row.line.newText;
	}
}
