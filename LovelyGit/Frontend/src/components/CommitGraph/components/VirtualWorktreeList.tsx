import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import type { RepositoryWorktreeItem } from "@/generated/types";
import type { WorktreeMutationController } from "../hooks/useWorktreeMutations";
import { WorktreeRow } from "./WorktreeSection";

const WORKTREE_ROW_HEIGHT = 38;

export function VirtualWorktreeList({
	controller,
	worktrees,
}: {
	controller: WorktreeMutationController;
	worktrees: RepositoryWorktreeItem[];
}) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: worktrees.length,
		estimateSize: () => WORKTREE_ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: 6,
	});

	return (
		<div
			className="custom-scrollbar h-full overflow-y-auto"
			data-worktree-list="virtual"
			ref={scrollRef}
		>
			<div className="relative" style={{ height: virtualizer.getTotalSize() }}>
				{virtualizer.getVirtualItems().map((virtualRow) => {
					const worktree = worktrees[virtualRow.index];
					return worktree ? (
						<div
							className="absolute inset-x-0 pb-1"
							key={worktree.path}
							style={{ transform: `translateY(${virtualRow.start}px)` }}
						>
							<WorktreeRow controller={controller} worktree={worktree} />
						</div>
					) : null;
				})}
			</div>
		</div>
	);
}
