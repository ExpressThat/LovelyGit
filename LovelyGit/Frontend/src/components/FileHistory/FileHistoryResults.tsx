import { useVirtualizer } from "@tanstack/react-virtual";
import { type ReactNode, useEffect, useRef } from "react";
import type { FileHistoryResult } from "@/generated/types";
import { FileHistoryResultRow } from "./FileHistoryResultRow";

const ROW_HEIGHT = 76;
const INITIAL_WINDOW = 10;

export function FileHistoryResults({
	activeIndex,
	emptyState,
	footer,
	onSelect,
	onSelectIndex,
	results,
}: {
	activeIndex: number;
	emptyState: ReactNode;
	footer: ReactNode;
	onSelect: (index: number) => void;
	onSelectIndex: (index: number) => void;
	results: FileHistoryResult[];
}) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: results.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: 5,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(results.length, INITIAL_WINDOW) },
					(_, index) => ({ index, start: index * ROW_HEIGHT }),
				);

	useEffect(() => {
		if (results.length > 0)
			virtualizer.scrollToIndex(activeIndex, { align: "auto" });
	}, [activeIndex, results.length, virtualizer]);

	return (
		<div
			className="custom-scrollbar h-[min(58vh,520px)] overflow-y-auto p-2"
			ref={scrollRef}
		>
			{emptyState}
			{results.length > 0 ? (
				<div
					className="relative"
					style={{ height: `${virtualizer.getTotalSize()}px` }}
				>
					{visibleRows.map((virtualRow) => {
						const result = results[virtualRow.index];
						return result ? (
							<div
								className="absolute left-0 right-0 top-0"
								data-index={virtualRow.index}
								key={result.hash}
								ref={virtualizer.measureElement}
								style={{ transform: `translateY(${virtualRow.start}px)` }}
							>
								<FileHistoryResultRow
									index={virtualRow.index}
									isSelected={virtualRow.index === activeIndex}
									onSelect={() => onSelect(virtualRow.index)}
									onSelectIndex={() => onSelectIndex(virtualRow.index)}
									result={result}
								/>
							</div>
						) : null;
					})}
				</div>
			) : null}
			{footer}
		</div>
	);
}
