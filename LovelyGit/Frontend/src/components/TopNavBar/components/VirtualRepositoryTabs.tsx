import { useVirtualizer } from "@tanstack/react-virtual";
import type { ReactNode } from "react";
import { useEffect, useRef } from "react";
import type { KnownGitRepository } from "@/generated/types";

const ESTIMATED_TAB_WIDTH = 160;
const INITIAL_WINDOW = 8;

export function VirtualRepositoryTabs({
	currentRepositoryId,
	renderTab,
	repositories,
}: {
	currentRepositoryId: string | null;
	renderTab: (repository: KnownGitRepository) => ReactNode;
	repositories: KnownGitRepository[];
}) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const activeIndex = repositories.findIndex(
		(repository) => repository.id === currentRepositoryId,
	);
	const virtualizer = useVirtualizer({
		count: repositories.length,
		estimateSize: () => ESTIMATED_TAB_WIDTH,
		getScrollElement: () => scrollRef.current,
		horizontal: true,
		initialOffset: Math.max(0, activeIndex) * ESTIMATED_TAB_WIDTH,
		overscan: 3,
	});
	useEffect(() => {
		if (activeIndex >= 0)
			virtualizer.scrollToIndex(activeIndex, { align: "auto" });
	}, [activeIndex, virtualizer]);
	const measuredTabs = virtualizer.getVirtualItems();
	const initialStart = Math.min(
		Math.max(0, activeIndex - Math.floor(INITIAL_WINDOW / 2)),
		Math.max(0, repositories.length - INITIAL_WINDOW),
	);
	const visibleTabs =
		measuredTabs.length > 0
			? measuredTabs
			: Array.from(
					{ length: Math.min(INITIAL_WINDOW, repositories.length) },
					(_, offset) => {
						const index = initialStart + offset;
						return { index, start: index * ESTIMATED_TAB_WIDTH };
					},
				);

	return (
		<div
			className="custom-scrollbar min-w-0 flex-1 overflow-x-auto overflow-y-hidden"
			data-repository-tabs="virtual"
			ref={scrollRef}
		>
			<div
				className="relative h-8"
				style={{ width: virtualizer.getTotalSize() }}
			>
				{visibleTabs.map((tab) => {
					const repository = repositories[tab.index];
					return repository ? (
						<div
							className="absolute bottom-0 left-0"
							data-index={tab.index}
							key={repository.id}
							ref={virtualizer.measureElement}
							style={{ transform: `translateX(${tab.start}px)` }}
						>
							{renderTab(repository)}
						</div>
					) : null;
				})}
			</div>
		</div>
	);
}
