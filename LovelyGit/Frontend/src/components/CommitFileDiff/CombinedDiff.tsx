import { useVirtualizer } from "@tanstack/react-virtual";
import { useMemo, useRef, useState } from "react";
import type { CommitFileDiffLine } from "@/generated/types";
import {
	CodeCell,
	changeMarker,
	estimateCodeWidth,
	LineNumber,
	lineBackground,
} from "./DiffLineRendering";
import {
	DiffChunkSeparator,
	type DiffDisplayRow,
	DiffLineActionButton,
	getCombinedLineAction,
} from "./DiffRows";

const DIFF_OVERSCAN = 12;

export function CombinedDiff({
	isLineActionBusy = false,
	lines,
	onStageLine,
	onUnstageLine,
	wrapLines,
}: {
	isLineActionBusy?: boolean;
	lines: DiffDisplayRow[];
	onStageLine?: (line: CommitFileDiffLine) => void;
	onUnstageLine?: (line: CommitFileDiffLine) => void;
	wrapLines: boolean;
}) {
	const hasLineAction = Boolean(onStageLine || onUnstageLine);
	const viewportRef = useRef<HTMLDivElement | null>(null);
	const scrollerRef = useRef<HTMLDivElement | null>(null);
	const [scrollLeft, setScrollLeft] = useState(0);
	const contentWidth = useMemo(
		() =>
			estimateCodeWidth(
				lines.map((row) => (row.kind === "line" ? (row.line.text ?? "") : "")),
			),
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
						const lineAction = getCombinedLineAction(
							line,
							onStageLine,
							onUnstageLine,
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
									width={contentWidth}
									wrapLines={wrapLines}
								/>
								{lineAction ? (
									<DiffLineActionButton
										action={lineAction}
										disabled={isLineActionBusy}
										line={line}
									/>
								) : hasLineAction ? (
									<div className="border-l" />
								) : null}
							</div>
						);
					})}
				</div>
			</div>
			{wrapLines ? null : (
				<div
					className="custom-scrollbar h-3 shrink-0 overflow-x-auto overflow-y-hidden border-t bg-background"
					onScroll={(event) => setScrollLeft(event.currentTarget.scrollLeft)}
					ref={scrollerRef}
				>
					<div style={{ height: 1, width: contentWidth }} />
				</div>
			)}
		</div>
	);
}
