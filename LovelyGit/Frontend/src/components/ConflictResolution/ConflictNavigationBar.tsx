import {
	Ban,
	ChevronDown,
	ChevronUp,
	GripHorizontal,
	RotateCcw,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";

export function ConflictNavigationBar({
	active,
	count,
	disabled,
	onNavigate,
	onOmit,
	onPointerDown,
	onResizeBy,
	onReset,
	resetDisabled,
	unresolved,
}: {
	active: number;
	count: number;
	disabled: boolean;
	onNavigate: (index: number) => void;
	onOmit: () => void;
	onPointerDown: (event: React.PointerEvent<HTMLButtonElement>) => void;
	onResizeBy: (amount: number) => void;
	onReset: () => void;
	resetDisabled: boolean;
	unresolved: number;
}) {
	return (
		<div className="custom-scrollbar flex h-10 shrink-0 items-center gap-2 overflow-x-auto overflow-y-hidden border-y bg-card px-3">
			<button
				aria-label="Resize source and output panels"
				className="grid size-6 cursor-row-resize place-items-center rounded text-muted-foreground hover:bg-accent hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
				onKeyDown={(event) => {
					if (event.key === "ArrowUp") onResizeBy(-2);
					if (event.key === "ArrowDown") onResizeBy(2);
				}}
				onPointerDown={onPointerDown}
				type="button"
			>
				<GripHorizontal className="size-4" />
			</button>
			<span className="shrink-0 text-xs font-semibold">Output</span>
			<span className="shrink-0 text-[10px] text-muted-foreground">
				{count === 0
					? "No text conflicts"
					: `Conflict ${active + 1} of ${count}`}
			</span>
			<span className="shrink-0 rounded-full bg-muted px-2 py-0.5 text-[10px] text-muted-foreground">
				{unresolved} unresolved
			</span>
			<div className="ml-auto flex shrink-0 items-center gap-1">
				<Button
					aria-label="Previous conflict"
					disabled={disabled || count < 2}
					onClick={() => onNavigate((active - 1 + count) % count)}
					size="icon-xs"
					variant="ghost"
				>
					<ChevronUp />
				</Button>
				<Button
					aria-label="Next conflict"
					disabled={disabled || count < 2}
					onClick={() => onNavigate((active + 1) % count)}
					size="icon-xs"
					variant="ghost"
				>
					<ChevronDown />
				</Button>
				<Button
					disabled={disabled || count === 0}
					onClick={onOmit}
					size="xs"
					variant="ghost"
				>
					<Ban /> Omit conflict
				</Button>
				<Button
					disabled={resetDisabled}
					onClick={onReset}
					size="xs"
					variant="ghost"
				>
					<RotateCcw /> Reset
				</Button>
			</div>
		</div>
	);
}
