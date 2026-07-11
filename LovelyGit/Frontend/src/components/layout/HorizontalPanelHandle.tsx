import { GripVertical } from "lucide-react";

export function HorizontalPanelHandle({
	label,
	onPointerDown,
	onResizeBy,
	side,
}: {
	label: string;
	onPointerDown: (event: React.PointerEvent<HTMLButtonElement>) => void;
	onResizeBy: (amount: number) => void;
	side: "left" | "right";
}) {
	return (
		<button
			aria-label={label}
			className={`${side === "left" ? "-left-1" : "-right-1"} group absolute inset-y-0 z-20 w-2 cursor-col-resize focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring`}
			onKeyDown={(event) => {
				if (event.key === "ArrowLeft") onResizeBy(-16);
				if (event.key === "ArrowRight") onResizeBy(16);
			}}
			onPointerDown={onPointerDown}
			type="button"
		>
			<span className="absolute inset-y-0 left-1/2 w-px -translate-x-1/2 bg-border transition-colors group-hover:bg-primary group-focus-visible:bg-primary" />
			<GripVertical className="absolute left-1/2 top-1/2 size-3 -translate-x-1/2 -translate-y-1/2 rounded bg-sidebar text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100 group-focus-visible:opacity-100" />
		</button>
	);
}
