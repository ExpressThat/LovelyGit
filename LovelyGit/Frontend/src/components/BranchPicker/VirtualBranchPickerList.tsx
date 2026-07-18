import { Combobox } from "@base-ui/react/combobox";
import { useVirtualizer } from "@tanstack/react-virtual";
import { useEffect, useRef } from "react";
import { Check, Cloud, GitBranch } from "@/components/icons/lovelyIcons";
import { cn } from "@/lib/utils";

const ROW_HEIGHT = 32;
const MAX_HEIGHT = 256;
const INITIAL_WINDOW = 10;

type VirtualBranchPickerListProps = {
	activeBranch: string | undefined;
	kind: "local" | "remote";
	onActiveBranchChange: (branch: string) => void;
	options: string[];
	selected: string;
};

export function VirtualBranchPickerList({
	activeBranch,
	kind,
	onActiveBranchChange,
	options,
	selected,
}: VirtualBranchPickerListProps) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: options.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: 6,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(options.length, INITIAL_WINDOW) },
					(_, index) => ({ index, start: index * ROW_HEIGHT }),
				);

	useEffect(() => {
		const activeIndex = activeBranch
			? options.indexOf(activeBranch)
			: options.indexOf(selected);
		if (activeIndex >= 0) {
			virtualizer.scrollToIndex(activeIndex, { align: "auto" });
		}
	}, [activeBranch, options, selected, virtualizer]);

	const RefIcon = kind === "remote" ? Cloud : GitBranch;
	return (
		<Combobox.List
			className="custom-scrollbar overflow-y-auto p-1 outline-none"
			data-branch-picker-list="virtual"
			ref={scrollRef}
			style={{
				height: `${Math.min(MAX_HEIGHT, options.length * ROW_HEIGHT + 8)}px`,
			}}
		>
			<div
				className="relative"
				style={{ height: `${virtualizer.getTotalSize()}px` }}
			>
				{visibleRows.map((row) => {
					const branch = options[row.index];
					return branch ? (
						<Combobox.Item
							className={cn(
								"absolute inset-x-0 flex h-8 cursor-default items-center gap-2 rounded-md px-2 text-sm outline-hidden select-none data-highlighted:bg-accent data-highlighted:text-accent-foreground",
								branch === activeBranch && "bg-accent text-accent-foreground",
							)}
							index={row.index}
							key={branch}
							onMouseMove={() => onActiveBranchChange(branch)}
							style={{ transform: `translateY(${row.start}px)` }}
							value={branch}
						>
							<RefIcon aria-hidden="true" className="size-4 shrink-0" />
							<span className="min-w-0 flex-1 truncate">{branch}</span>
							{branch === selected ? (
								<Combobox.ItemIndicator>
									<Check aria-label="Selected branch" className="size-4" />
								</Combobox.ItemIndicator>
							) : null}
						</Combobox.Item>
					) : null;
				})}
			</div>
		</Combobox.List>
	);
}
