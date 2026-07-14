import { useVirtualizer } from "@tanstack/react-virtual";
import { type CSSProperties, type RefObject, useEffect, useMemo } from "react";
import type {
	CommitFileDiffResponse,
	ConflictHunk,
	ConflictSourceMetadata,
} from "@/generated/types";
import {
	ConflictPaneItem,
	ConflictSourceHeader,
} from "./ConflictSourcePaneItems";
import {
	buildConflictDiffItems,
	type ConflictDiffItem,
	type ConflictSide,
	estimateConflictPaneCodeWidth,
	estimateConflictPaneGutterWidth,
} from "./conflictDiffItems";
import type { ConflictChoice } from "./conflictDocument";
import { filterConflictLines } from "./conflictLineFilter";
import { useConflictDiffLines } from "./useConflictDiffLines";

export function ConflictSourcePane({
	activeConflict,
	baseText,
	choices,
	contextLines,
	diff,
	disabled,
	hunks,
	lineDisplayMode,
	metadata,
	onChoice,
	onScroll,
	side,
	sourceText,
	viewportRef,
	wrapLines,
}: {
	activeConflict: number;
	baseText: string;
	choices: Record<number, ConflictChoice>;
	contextLines: number;
	diff: CommitFileDiffResponse | null;
	disabled: boolean;
	hunks: ConflictHunk[];
	lineDisplayMode: "Changes" | "FullFile";
	metadata: ConflictSourceMetadata;
	onChoice: (id: number, choice: ConflictChoice) => void;
	onScroll: (side: ConflictSide, element: HTMLDivElement) => void;
	side: ConflictSide;
	sourceText: string;
	viewportRef: RefObject<HTMLDivElement | null>;
	wrapLines: boolean;
}) {
	const loaded = useConflictDiffLines(diff, baseText, sourceText);
	const items = useMemo(
		() =>
			loaded.status === "ready"
				? buildConflictDiffItems(
						lineDisplayMode === "Changes"
							? filterConflictLines(loaded.lines, hunks, side, contextLines)
							: loaded.lines,
						hunks,
						side,
					)
				: [],
		[contextLines, lineDisplayMode, loaded, hunks, side],
	);
	const width = useMemo(() => estimateConflictPaneCodeWidth(items), [items]);
	const gutterWidth = useMemo(
		() => estimateConflictPaneGutterWidth(items),
		[items],
	);
	const virtualizer = useVirtualizer({
		count: items.length,
		estimateSize: (index) => (items[index]?.kind === "hunk" ? 34 : 18),
		getScrollElement: () => viewportRef.current,
		initialRect: { width: 800, height: 400 },
		measureElement: (element) => element.getBoundingClientRect().height,
		overscan: 16,
	});
	useEffect(() => {
		const index = items.findIndex(
			(item) => item.kind === "hunk" && item.hunk.id === activeConflict,
		);
		if (index >= 0) virtualizer.scrollToIndex(index, { align: "start" });
	}, [activeConflict, items, virtualizer]);
	const virtualItems = virtualizer.getVirtualItems();
	const renderedItems =
		virtualItems.length > 0 ? virtualItems : fallbackVirtualItems(items);
	const totalHeight = Math.max(
		virtualizer.getTotalSize(),
		estimatedHeight(items),
	);

	return (
		<section
			aria-label={`${sideLabel(side)} source`}
			className="flex min-h-0 min-w-0 flex-1 flex-col"
			style={
				{
					"--conflict-gutter-width": `${gutterWidth}px`,
				} as CSSProperties
			}
		>
			<ConflictSourceHeader metadata={metadata} side={side} />
			<div className="grid h-6 shrink-0 grid-cols-[var(--conflict-gutter-width)_var(--conflict-gutter-width)_minmax(0,1fr)_2rem] border-b bg-card text-[9px] font-semibold uppercase text-muted-foreground">
				<span className="border-r px-2 py-1 text-right">Base</span>
				<span className="border-r px-2 py-1 text-right">Line</span>
				<span className="px-2 py-1">Content</span>
				<span />
			</div>
			<div
				className="custom-scrollbar min-h-0 flex-1 overflow-auto bg-background font-mono text-[12px] leading-[18px]"
				onScroll={(event) => onScroll(side, event.currentTarget)}
				ref={viewportRef}
			>
				{loaded.status === "loading" ? (
					<PaneMessage message="Preparing comparison…" />
				) : null}
				{loaded.status === "error" ? (
					<PaneMessage message={loaded.message} />
				) : null}
				<div
					className="relative"
					style={{
						height: totalHeight,
						minWidth: wrapLines ? undefined : width + 128,
					}}
				>
					{renderedItems.map((virtualItem) => (
						<div
							className="absolute left-0 top-0 w-full"
							data-index={virtualItem.index}
							key={`${virtualItem.index}:${itemKey(items[virtualItem.index])}`}
							ref={virtualizer.measureElement}
							style={{ transform: `translateY(${virtualItem.start}px)` }}
						>
							<ConflictPaneItem
								{...{ choices, disabled, onChoice, side, wrapLines }}
								item={items[virtualItem.index]}
							/>
						</div>
					))}
				</div>
			</div>
		</section>
	);
}

function PaneMessage({ message }: { message: string }) {
	return (
		<div className="p-4 font-sans text-xs text-muted-foreground">{message}</div>
	);
}

function sideLabel(side: ConflictSide) {
	return side === "ours" ? "Current" : "Incoming";
}

function itemKey(item: ConflictDiffItem | undefined) {
	return item?.kind === "hunk"
		? `hunk:${item.hunk.id}`
		: (item?.key ?? "missing");
}

function fallbackVirtualItems(items: ConflictDiffItem[]) {
	let start = 0;
	return items.slice(0, 200).map((item, index) => {
		const size = item.kind === "hunk" ? 34 : 18;
		const virtualItem = { index, key: index, start, size };
		start += size;
		return virtualItem;
	});
}

function estimatedHeight(items: ConflictDiffItem[]) {
	return items.reduce(
		(height, item) => height + (item.kind === "hunk" ? 34 : 18),
		0,
	);
}
