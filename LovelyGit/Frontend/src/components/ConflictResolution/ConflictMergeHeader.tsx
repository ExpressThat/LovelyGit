import { FileWarning, Save, Wrench, X } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";

export function ConflictMergeHeader({
	conflictCount,
	controlsDisabled,
	fileName,
	isBusy,
	isExternalOpen,
	onClose,
	onExternalTool,
	onSave,
	saveDisabled,
}: {
	conflictCount: number;
	controlsDisabled: boolean;
	fileName: string;
	isBusy: boolean;
	isExternalOpen: boolean;
	onClose: () => void;
	onExternalTool: () => void;
	onSave: () => void;
	saveDisabled: boolean;
}) {
	return (
		<header className="custom-scrollbar flex h-12 shrink-0 items-center gap-3 overflow-x-auto overflow-y-hidden border-b bg-popover px-3">
			<FileWarning className="size-4 text-amber-500" />
			<div className="min-w-0">
				<div className="truncate text-xs font-semibold">{fileName}</div>
				<div className="text-[10px] text-muted-foreground">
					{conflictCount} {conflictCount === 1 ? "conflict" : "conflicts"}
				</div>
			</div>
			<span className="ml-auto shrink-0 rounded border bg-card px-2 py-1 text-[9px] font-semibold uppercase tracking-wide text-muted-foreground">
				UTF-8
			</span>
			<Button
				className="shrink-0"
				disabled={controlsDisabled || isBusy}
				onClick={onExternalTool}
				size="sm"
				variant="outline"
			>
				<Wrench />{" "}
				{isExternalOpen ? "Merge tool open…" : "Open external merge tool"}
			</Button>
			<Button
				aria-label="Save & stage"
				className="shrink-0"
				disabled={saveDisabled || isBusy}
				onClick={onSave}
				size="sm"
			>
				<Save /> {isBusy ? "Working…" : "Save & stage"}
			</Button>
			<Button
				aria-label="Close conflict resolver"
				className="shrink-0"
				disabled={isBusy}
				onClick={onClose}
				size="icon-sm"
				variant="ghost"
			>
				<X />
			</Button>
		</header>
	);
}
