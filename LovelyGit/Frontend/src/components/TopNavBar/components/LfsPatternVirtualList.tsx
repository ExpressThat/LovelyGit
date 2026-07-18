import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import { LfsPatternRow } from "./LfsPatternRow";

const ROW_HEIGHT = 54;
const ROW_GAP = 8;
const INITIAL_WINDOW = 10;
const ANIMATED_ROW_LIMIT = 20;

export function LfsPatternVirtualList({
	busyPattern,
	disabled,
	onRemove,
	patterns,
}: {
	busyPattern: string | null;
	disabled: boolean;
	onRemove: (pattern: string) => void;
	patterns: string[];
}) {
	const scrollRef = useRef<HTMLElement>(null);
	const virtualizer = useVirtualizer({
		count: patterns.length,
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
					{ length: Math.min(patterns.length, INITIAL_WINDOW) },
					(_, index) => ({ index, start: index * (ROW_HEIGHT + ROW_GAP) }),
				);
	const animateRows = patterns.length <= ANIMATED_ROW_LIMIT;

	return (
		<section
			aria-label="Tracked Git LFS patterns"
			className="custom-scrollbar h-full min-h-20 overflow-y-auto"
			ref={scrollRef}
		>
			<div
				className="relative min-w-0"
				style={{ height: `${virtualizer.getTotalSize()}px` }}
			>
				{visibleRows.map((row) => {
					const pattern = patterns[row.index];
					return pattern ? (
						<LfsPatternRow
							animateRow={animateRows}
							busy={busyPattern === pattern}
							disabled={disabled}
							key={pattern}
							onRemove={() => onRemove(pattern)}
							pattern={pattern}
							position={{
								index: row.index,
								measureElement: virtualizer.measureElement,
								start: row.start,
							}}
						/>
					) : null;
				})}
			</div>
		</section>
	);
}
