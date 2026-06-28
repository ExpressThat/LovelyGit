import { useVirtualizer } from "@tanstack/react-virtual";
import { useMemo, useRef, useState } from "react";
import type { CommitFileDiffLine } from "@/generated/types";
import { estimateCodeWidth } from "./DiffLineRendering";
import {
	DiffChunkSeparator,
	type DiffDisplayRow,
	getSideBySideLineAction,
} from "./DiffRows";
import { DiffPaneHeader, SideBySideRow } from "./SideBySideRow";

const DIFF_OVERSCAN = 12;

export function SideBySideDiff({
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
	const oldScrollerRef = useRef<HTMLDivElement | null>(null);
	const newScrollerRef = useRef<HTMLDivElement | null>(null);
	const syncingRef = useRef(false);
	const [oldScrollLeft, setOldScrollLeft] = useState(0);
	const [newScrollLeft, setNewScrollLeft] = useState(0);
	const contentWidth = useMemo(
		() =>
			estimateCodeWidth(
				lines.flatMap((row) =>
					row.kind === "line"
						? [row.line.oldText ?? "", row.line.newText ?? ""]
						: [],
				),
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

	const syncBottomScroll = (
		source: "old" | "new",
		event: React.UIEvent<HTMLDivElement>,
	) => {
		if (syncingRef.current) {
			return;
		}

		const nextScrollLeft = event.currentTarget.scrollLeft;
		const target =
			source === "old" ? newScrollerRef.current : oldScrollerRef.current;
		if (!target) {
			return;
		}

		syncingRef.current = true;
		target.scrollLeft = nextScrollLeft;
		setOldScrollLeft(nextScrollLeft);
		setNewScrollLeft(nextScrollLeft);
		requestAnimationFrame(() => {
			syncingRef.current = false;
		});
	};

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
											line={line.line}
											isLineActionBusy={isLineActionBusy}
											lineAction={getSideBySideLineAction(
												line.line,
												"old",
												onStageLine,
												onUnstageLine,
											)}
											rowHeight={item.size}
											scrollLeft={oldScrollLeft}
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
											line={line.line}
											isLineActionBusy={isLineActionBusy}
											lineAction={getSideBySideLineAction(
												line.line,
												"new",
												onStageLine,
												onUnstageLine,
											)}
											rowHeight={item.size}
											scrollLeft={newScrollLeft}
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
						onScroll={(event) => syncBottomScroll("old", event)}
						ref={oldScrollerRef}
					>
						<div style={{ height: 1, width: contentWidth }} />
					</div>
					<div
						className="custom-scrollbar overflow-x-auto overflow-y-hidden"
						onScroll={(event) => syncBottomScroll("new", event)}
						ref={newScrollerRef}
					>
						<div style={{ height: 1, width: contentWidth }} />
					</div>
				</div>
			)}
		</div>
	);
}
