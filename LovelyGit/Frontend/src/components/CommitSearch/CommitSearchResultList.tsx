import { useVirtualizer } from "@tanstack/react-virtual";
import { useEffect, useRef } from "react";
import type { CommitSearchResult } from "@/generated/types";
import { CommitSearchResultRow } from "./CommitSearchResultRow";

const ROW_HEIGHT = 76;
const ROW_GAP = 4;
const INITIAL_WINDOW = 10;

export function CommitSearchResultList({
	onSelect,
	onSelectIndex,
	query,
	results,
	selectedIndex,
}: {
	onSelect: (index: number) => void;
	onSelectIndex: (index: number) => void;
	query: string;
	results: CommitSearchResult[];
	selectedIndex: number;
}) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: results.length,
		estimateSize: () => ROW_HEIGHT,
		gap: ROW_GAP,
		getScrollElement: () => scrollRef.current,
		overscan: 5,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(results.length, INITIAL_WINDOW) },
					(_, index) => ({ index, start: index * (ROW_HEIGHT + ROW_GAP) }),
				);

	useEffect(() => {
		virtualizer.scrollToIndex(selectedIndex, { align: "auto" });
	}, [selectedIndex, virtualizer]);

	return (
		<div
			className="custom-scrollbar min-h-0 flex-1 overflow-y-auto"
			ref={scrollRef}
		>
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
							<CommitSearchResultRow
								index={virtualRow.index}
								isSelected={selectedIndex === virtualRow.index}
								onSelect={() => onSelect(virtualRow.index)}
								onSelectIndex={() => onSelectIndex(virtualRow.index)}
								query={query}
								result={result}
							/>
						</div>
					) : null;
				})}
			</div>
		</div>
	);
}
