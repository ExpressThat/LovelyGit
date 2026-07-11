import { ChevronDown, ChevronRight, GripHorizontal, Plus } from "lucide-react";
import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";

export function RefsAccordionPane({
	children,
	count,
	id,
	isOpen,
	onCreateWorktree,
	onToggle,
	weight,
}: {
	children: ReactNode;
	count: number;
	id: string;
	isOpen: boolean;
	onCreateWorktree?: () => void;
	onToggle: () => void;
	weight: number;
}) {
	return (
		<section
			className="flex min-h-0 flex-col overflow-hidden"
			data-refs-pane={id}
			style={
				isOpen ? { flexBasis: 0, flexGrow: weight, minHeight: 72 } : undefined
			}
		>
			<header className="flex h-7 shrink-0 items-center gap-1 border-b px-1 text-[10px] font-semibold uppercase text-muted-foreground">
				<Button
					aria-expanded={isOpen}
					aria-label={`${isOpen ? "Collapse" : "Expand"} ${id}`}
					className="size-5 rounded-md"
					onClick={onToggle}
					size="icon-xs"
					variant="ghost"
				>
					{isOpen ? <ChevronDown /> : <ChevronRight />}
				</Button>
				<button
					className="min-w-0 flex-1 truncate text-left"
					onClick={onToggle}
					type="button"
				>
					{id}
				</button>
				<span>{count}</span>
				{onCreateWorktree ? (
					<Button
						aria-label="Create worktree"
						className="size-5"
						onClick={onCreateWorktree}
						size="icon-xs"
						variant="ghost"
					>
						<Plus aria-hidden="true" />
					</Button>
				) : null}
			</header>
			{isOpen ? (
				<div className="custom-scrollbar min-h-0 flex-1 overflow-y-auto p-2">
					{children}
				</div>
			) : null}
		</section>
	);
}

export function RefsPaneSplitter({
	onPointerDown,
	onResizeBy,
}: {
	onPointerDown: (event: React.PointerEvent<HTMLButtonElement>) => void;
	onResizeBy: (amount: number) => void;
}) {
	return (
		<button
			aria-label="Resize reference sections"
			className="group relative z-10 h-1.5 shrink-0 cursor-row-resize border-y border-transparent hover:border-primary/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
			onKeyDown={(event) => {
				if (event.key === "ArrowUp") onResizeBy(-0.1);
				if (event.key === "ArrowDown") onResizeBy(0.1);
			}}
			onPointerDown={onPointerDown}
			type="button"
		>
			<GripHorizontal className="absolute left-1/2 top-1/2 size-3 -translate-x-1/2 -translate-y-1/2 text-muted-foreground opacity-0 group-hover:opacity-100 group-focus-visible:opacity-100" />
		</button>
	);
}
