import { useVirtualizer } from "@tanstack/react-virtual";
import { useEffect, useRef } from "react";
import { Check, GitBranch } from "@/components/icons/lovelyIcons";
import { DropdownMenuItem } from "@/components/ui/dropdown-menu";
import type { RepositoryRefItem } from "@/generated/types";
import { cn } from "@/lib/utils";

const ROW_HEIGHT = 32;
const MAX_HEIGHT = 256;
const INITIAL_WINDOW = 10;

type VirtualBranchMenuListProps = {
	activeIndex: number;
	branches: RepositoryRefItem[];
	busy: boolean;
	currentBranchName: string | null;
	onActiveIndexChange: (index: number) => void;
	onCheckout: (branchName: string) => void;
};

export function VirtualBranchMenuList({
	activeIndex,
	branches,
	busy,
	currentBranchName,
	onActiveIndexChange,
	onCheckout,
}: VirtualBranchMenuListProps) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: branches.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: 6,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(branches.length, INITIAL_WINDOW) },
					(_, index) => ({ index, start: index * ROW_HEIGHT }),
				);

	useEffect(() => {
		if (branches.length > 0) {
			virtualizer.scrollToIndex(activeIndex, { align: "auto" });
		}
	}, [activeIndex, branches.length, virtualizer]);

	return (
		<div
			className="custom-scrollbar overflow-y-auto p-1"
			data-branch-menu-list="virtual"
			id="branch-switcher-results"
			ref={scrollRef}
			style={{
				height: `${Math.min(MAX_HEIGHT, branches.length * ROW_HEIGHT + 8)}px`,
			}}
		>
			<div
				className="relative"
				style={{ height: `${virtualizer.getTotalSize()}px` }}
			>
				{visibleRows.map((row) => {
					const branch = branches[row.index];
					return branch ? (
						<DropdownMenuItem
							className={cn(
								"absolute inset-x-0 h-8 px-2",
								row.index === activeIndex && "bg-accent text-accent-foreground",
							)}
							disabled={busy}
							id={`branch-switcher-item-${row.index}`}
							key={branch.name}
							onClick={() => onCheckout(branch.name)}
							onMouseMove={() => onActiveIndexChange(row.index)}
							style={{ transform: `translateY(${row.start}px)` }}
						>
							<GitBranch aria-hidden="true" className="size-4" />
							<span className="min-w-0 flex-1 truncate">{branch.name}</span>
							{branch.name === currentBranchName ? (
								<Check
									aria-label="Current branch"
									className="size-4 text-primary"
								/>
							) : null}
						</DropdownMenuItem>
					) : null;
				})}
			</div>
		</div>
	);
}
