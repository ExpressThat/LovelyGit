import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import type { KnownGitRepository } from "@/generated/types";
import { RecentRepositoryRow } from "./RecentRepositoryRow";

const ESTIMATED_ROW_HEIGHT = 48;
const INITIAL_WINDOW = 12;
const MAX_HEIGHT = 480;

export function VirtualRecentRepositoryList({
	onOpen,
	onRemove,
	repositories,
}: {
	onOpen: (repositoryId: string) => void;
	onRemove: (repositoryId: string) => Promise<void>;
	repositories: KnownGitRepository[];
}) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: repositories.length,
		estimateSize: () => ESTIMATED_ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: 4,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(INITIAL_WINDOW, repositories.length) },
					(_, index) => ({ index, start: index * ESTIMATED_ROW_HEIGHT }),
				);

	return (
		<div
			className="custom-scrollbar overflow-y-auto"
			data-recent-repositories="virtual"
			ref={scrollRef}
			style={{
				height: Math.min(
					MAX_HEIGHT,
					repositories.length * ESTIMATED_ROW_HEIGHT,
				),
			}}
		>
			<div className="relative" style={{ height: virtualizer.getTotalSize() }}>
				{visibleRows.map((row) => {
					const repository = repositories[row.index];
					return repository ? (
						<div
							className="absolute inset-x-0 pb-1"
							data-index={row.index}
							key={repository.id}
							ref={virtualizer.measureElement}
							style={{ transform: `translateY(${row.start}px)` }}
						>
							<RecentRepositoryRow
								onOpen={() => onOpen(repository.id)}
								onRemove={() => onRemove(repository.id)}
								repository={repository}
							/>
						</div>
					) : null;
				})}
			</div>
		</div>
	);
}
