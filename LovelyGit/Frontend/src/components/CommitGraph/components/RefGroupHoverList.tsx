import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import type { RefGroup } from "./RefCellUtils";
import { RefGroupPill, type RefGroupPillProps } from "./RefGroupPill";

const ROW_HEIGHT = 15;
const MAX_HEIGHT = 288;
const INITIAL_WINDOW = 12;

export function RefGroupHoverList({
	groups,
	...pillProps
}: Omit<RefGroupPillProps, "group"> & { groups: RefGroup[] }) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: groups.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: 6,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(groups.length, INITIAL_WINDOW) },
					(_, index) => ({ index, start: index * ROW_HEIGHT }),
				);

	return (
		<div
			className="custom-scrollbar overflow-y-auto"
			data-ref-hover-list="virtual"
			ref={scrollRef}
			style={{ height: Math.min(MAX_HEIGHT, groups.length * ROW_HEIGHT) }}
		>
			<div className="relative" style={{ height: virtualizer.getTotalSize() }}>
				{visibleRows.map((row) => {
					const group = groups[row.index];
					return group ? (
						<div
							className="absolute inset-x-0 h-[15px]"
							key={group.key}
							style={{ transform: `translateY(${row.start}px)` }}
						>
							<RefGroupPill {...pillProps} group={group} wide />
						</div>
					) : null;
				})}
			</div>
		</div>
	);
}
