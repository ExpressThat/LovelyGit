import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import type { GitSubmodule, SubmoduleAction } from "@/generated/types";
import { SubmoduleRow } from "./SubmoduleRow";

const ROW_HEIGHT = 132;
const ROW_GAP = 8;
const INITIAL_WINDOW = 10;
const ANIMATED_ROW_LIMIT = 20;

export function SubmoduleVirtualList({
	busyPath,
	onDeinitialize,
	onRun,
	submodules,
}: {
	busyPath: string | null;
	onDeinitialize: (path: string) => void;
	onRun: (path: string, action: SubmoduleAction) => void;
	submodules: GitSubmodule[];
}) {
	const scrollRef = useRef<HTMLElement>(null);
	const virtualizer = useVirtualizer({
		count: submodules.length,
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
					{ length: Math.min(submodules.length, INITIAL_WINDOW) },
					(_, index) => ({ index, start: index * (ROW_HEIGHT + ROW_GAP) }),
				);
	const animateRows = submodules.length <= ANIMATED_ROW_LIMIT;

	return (
		<section
			aria-label="Configured submodules"
			className="custom-scrollbar h-full min-h-24 overflow-y-auto"
			ref={scrollRef}
		>
			<div
				className="relative"
				style={{ height: `${virtualizer.getTotalSize()}px` }}
			>
				{visibleRows.map((row) => {
					const submodule = submodules[row.index];
					return submodule ? (
						<SubmoduleRow
							animateRow={animateRows}
							busy={busyPath === submodule.path}
							disabled={busyPath !== null}
							key={submodule.path}
							onDeinitialize={() => onDeinitialize(submodule.path)}
							onRun={(action) => onRun(submodule.path, action)}
							position={{
								index: row.index,
								measureElement: virtualizer.measureElement,
								start: row.start,
							}}
							submodule={submodule}
						/>
					) : null;
				})}
			</div>
		</section>
	);
}
