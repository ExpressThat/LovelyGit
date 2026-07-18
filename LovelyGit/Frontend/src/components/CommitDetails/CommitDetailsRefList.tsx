import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import type { RepositoryRefItem } from "@/generated/types";
import { CommitDetailsRefPill } from "./CommitDetailsRefPill";

const ROW_HEIGHT = 28;
const MAX_HEIGHT = 252;
const INITIAL_WINDOW = 10;

export function CommitDetailsRefList({ refs }: { refs: RepositoryRefItem[] }) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: refs.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: 5,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(refs.length, INITIAL_WINDOW) },
					(_, index) => ({ index, start: index * ROW_HEIGHT }),
				);

	return (
		<div
			className="custom-scrollbar overflow-y-auto rounded-md border bg-background p-1"
			data-commit-details-ref-list="virtual"
			ref={scrollRef}
			style={{ height: Math.min(MAX_HEIGHT, refs.length * ROW_HEIGHT + 8) }}
		>
			<div className="relative" style={{ height: virtualizer.getTotalSize() }}>
				{visibleRows.map((row) => {
					const ref = refs[row.index];
					return ref ? (
						<div
							className="absolute inset-x-0 flex h-7 items-center px-1"
							key={`${ref.kind}:${ref.name}`}
							style={{ transform: `translateY(${row.start}px)` }}
						>
							<CommitDetailsRefPill refItem={ref} wide />
						</div>
					) : null;
				})}
			</div>
		</div>
	);
}
