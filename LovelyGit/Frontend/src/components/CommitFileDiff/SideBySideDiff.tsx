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

						return (
							<div
								className="absolute left-0 top-0 grid w-full grid-cols-2"
								data-index={item.index}
								key={item.key}
								ref={virtualizer.measureElement}
								style={{ transform: `translateY(${item.start}px)` }}
							>
								<div className="min-w-0 border-r">
									<SideBySideRow
										hunkAction={getSideDiffHunkAction(
											row.line,
											"old",
											hunkLookup,
											onStageHunk,
											onUnstageHunk,
										)}
										isLineActionBusy={isLineActionBusy}
										line={row.line}
										lineAction={getSideBySideLineAction(
											row.line,
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
								<SideBySideRow
									hunkAction={getSideDiffHunkAction(
										row.line,
										"new",
										hunkLookup,
										onStageHunk,
										onUnstageHunk,
									)}
									isLineActionBusy={isLineActionBusy}
									line={row.line}
									lineAction={getSideBySideLineAction(
										row.line,
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
