import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import type {
	InteractiveRebaseCommit,
	InteractiveRebasePlanItem,
} from "@/generated/types";
import { InteractiveRebasePlanRow } from "./InteractiveRebasePlanRow";

const ROW_HEIGHT = 50;
const ROW_GAP = 8;
const INITIAL_WINDOW = 8;

export function InteractiveRebasePlanList({
	commitByHash,
	onAction,
	onMessage,
	onMove,
	plan,
}: {
	commitByHash: Map<string, InteractiveRebaseCommit>;
	onAction: (hash: string, action: InteractiveRebasePlanItem["action"]) => void;
	onMessage: (hash: string, message: string) => void;
	onMove: (index: number, offset: number) => void;
	plan: InteractiveRebasePlanItem[];
}) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: plan.length,
		estimateSize: () => ROW_HEIGHT,
		gap: ROW_GAP,
		getScrollElement: () => scrollRef.current,
		overscan: 4,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(plan.length, INITIAL_WINDOW) },
					(_, index) => ({ index, start: index * (ROW_HEIGHT + ROW_GAP) }),
				);

	return (
		<div
			className="custom-scrollbar min-h-28 flex-1 overflow-y-auto pr-1"
			ref={scrollRef}
		>
			<ul
				className="relative"
				style={{ height: `${virtualizer.getTotalSize()}px` }}
			>
				{visibleRows.map((virtualRow) => {
					const item = plan[virtualRow.index];
					const commit = item ? commitByHash.get(item.hash) : null;
					return item && commit ? (
						<InteractiveRebasePlanRow
							commit={commit}
							containerRef={virtualizer.measureElement}
							index={virtualRow.index}
							item={item}
							key={item.hash}
							onAction={(action) => onAction(item.hash, action)}
							onMessage={(message) => onMessage(item.hash, message)}
							onMove={(offset) => onMove(virtualRow.index, offset)}
							offset={virtualRow.start}
							total={plan.length}
						/>
					) : null;
				})}
			</ul>
		</div>
	);
}
