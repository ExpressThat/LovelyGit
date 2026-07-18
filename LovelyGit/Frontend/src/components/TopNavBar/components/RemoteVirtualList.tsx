import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import type { GitRemote } from "@/generated/types";
import { RemoteRow } from "./RemoteRow";

const ROW_HEIGHT = 84;
const ROW_GAP = 8;
const INITIAL_WINDOW = 10;
const ANIMATED_ROW_LIMIT = 20;

export function RemoteVirtualList({
	disabled,
	onEdit,
	onRemove,
	remotes,
}: {
	disabled: boolean;
	onEdit: (remote: GitRemote) => void;
	onRemove: (remote: GitRemote) => void;
	remotes: GitRemote[];
}) {
	const scrollRef = useRef<HTMLElement>(null);
	const virtualizer = useVirtualizer({
		count: remotes.length,
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
					{ length: Math.min(remotes.length, INITIAL_WINDOW) },
					(_, index) => ({ index, start: index * (ROW_HEIGHT + ROW_GAP) }),
				);
	const animateRows = remotes.length <= ANIMATED_ROW_LIMIT;

	return (
		<section
			aria-label="Configured remotes"
			className="custom-scrollbar min-h-0 max-h-72 overflow-y-auto pr-1"
			ref={scrollRef}
		>
			<ul
				className="relative min-w-0"
				style={{ height: `${virtualizer.getTotalSize()}px` }}
			>
				{visibleRows.map((row) => {
					const remote = remotes[row.index];
					return remote ? (
						<RemoteRow
							animateRow={animateRows}
							disabled={disabled}
							key={remote.name}
							onEdit={() => onEdit(remote)}
							onRemove={() => onRemove(remote)}
							position={{
								index: row.index,
								measureElement: virtualizer.measureElement,
								start: row.start,
							}}
							remote={remote}
						/>
					) : null;
				})}
			</ul>
		</section>
	);
}
