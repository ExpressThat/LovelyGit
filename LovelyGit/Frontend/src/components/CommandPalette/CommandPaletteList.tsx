import { useVirtualizer } from "@tanstack/react-virtual";
import { useEffect, useRef } from "react";
import { motion, useReducedMotion } from "@/lib/motion";
import { cn } from "@/lib/utils";
import type { PaletteItem } from "./commandPaletteItems";

const ESTIMATED_ROW_HEIGHT = 52;
const INITIAL_WINDOW = 10;
const VIRTUAL_ITEM_THRESHOLD = 30;

export function CommandPaletteList({
	activeIndex,
	items,
	onActiveIndexChange,
}: {
	activeIndex: number;
	items: PaletteItem[];
	onActiveIndexChange: (index: number) => void;
}) {
	if (items.length === 0) {
		return (
			<div className="px-3 py-8 text-center text-sm text-muted-foreground">
				No matching commands
			</div>
		);
	}
	if (items.length <= VIRTUAL_ITEM_THRESHOLD) {
		return items.map((item, index) => (
			<PaletteRow
				active={activeIndex === index}
				item={item}
				key={item.id}
				onHover={() => onActiveIndexChange(index)}
			/>
		));
	}
	return (
		<VirtualPaletteItems
			activeIndex={activeIndex}
			items={items}
			onActiveIndexChange={onActiveIndexChange}
		/>
	);
}

function VirtualPaletteItems({
	activeIndex,
	items,
	onActiveIndexChange,
}: {
	activeIndex: number;
	items: PaletteItem[];
	onActiveIndexChange: (index: number) => void;
}) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: items.length,
		estimateSize: () => ESTIMATED_ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		initialOffset: activeIndex * ESTIMATED_ROW_HEIGHT,
		overscan: 3,
	});
	useEffect(() => {
		virtualizer.scrollToIndex(activeIndex, { align: "auto" });
	}, [activeIndex, virtualizer]);
	const measuredRows = virtualizer.getVirtualItems();
	const initialStart = Math.min(
		Math.max(0, activeIndex - Math.floor(INITIAL_WINDOW / 2)),
		Math.max(0, items.length - INITIAL_WINDOW),
	);
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(INITIAL_WINDOW, items.length) },
					(_, offset) => {
						const index = initialStart + offset;
						return { index, start: index * ESTIMATED_ROW_HEIGHT };
					},
				);

	return (
		<div
			className="custom-scrollbar h-[min(420px,60vh)] overflow-y-auto"
			data-command-palette-list="virtual"
			ref={scrollRef}
		>
			<div className="relative" style={{ height: virtualizer.getTotalSize() }}>
				{visibleRows.map((row) => {
					const item = items[row.index];
					return item ? (
						<div
							className="absolute inset-x-0"
							data-index={row.index}
							key={item.id}
							ref={virtualizer.measureElement}
							style={{ transform: `translateY(${row.start}px)` }}
						>
							<PaletteRow
								active={activeIndex === row.index}
								item={item}
								onHover={() => onActiveIndexChange(row.index)}
							/>
						</div>
					) : null;
				})}
			</div>
		</div>
	);
}

function PaletteRow({
	active,
	item,
	onHover,
}: {
	active: boolean;
	item: PaletteItem;
	onHover: () => void;
}) {
	const reduceMotion = useReducedMotion();
	return (
		<motion.button
			aria-disabled={item.disabled}
			className={cn(
				"relative flex w-full items-center gap-3 rounded-lg px-3 py-2 text-left",
				active && "bg-accent text-accent-foreground",
				item.disabled && "opacity-40",
			)}
			data-command-palette-row
			disabled={item.disabled}
			onClick={item.run}
			onMouseEnter={onHover}
			type="button"
			whileTap={reduceMotion ? undefined : { scale: 0.99 }}
		>
			<item.icon
				aria-hidden="true"
				className="size-4 shrink-0 text-muted-foreground"
			/>
			<span className="min-w-0">
				<span className="block truncate text-sm font-medium">{item.label}</span>
				<span className="block truncate text-xs text-muted-foreground">
					{item.description}
				</span>
			</span>
		</motion.button>
	);
}
