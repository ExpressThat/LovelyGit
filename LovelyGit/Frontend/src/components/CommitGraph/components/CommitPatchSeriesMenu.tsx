import {
	ClipboardCopy,
	Download,
	FileDiff,
} from "@/components/icons/lovelyIcons";
import { buttonVariants } from "@/components/ui/button";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuGroup,
	DropdownMenuItem,
	DropdownMenuLabel,
	DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

export function CommitPatchSeriesMenu({
	busyAction,
	count,
	onCopy,
	onSave,
}: {
	busyAction: "copy" | "save" | null;
	count: number;
	onCopy: () => void;
	onSave: () => void;
}) {
	const busy = busyAction !== null;
	return (
		<DropdownMenu>
			<DropdownMenuTrigger
				className={buttonVariants({ size: "xs", variant: "secondary" })}
				disabled={busy}
			>
				<FileDiff aria-hidden="true" />
				{busy
					? `${busyAction === "copy" ? "Copying" : "Saving"}…`
					: "Patch series"}
			</DropdownMenuTrigger>
			<DropdownMenuContent align="end" className="min-w-56">
				<DropdownMenuGroup>
					<DropdownMenuLabel>
						{count} commits · oldest to newest
					</DropdownMenuLabel>
					<DropdownMenuItem onClick={onCopy}>
						<ClipboardCopy aria-hidden="true" /> Copy patch series
					</DropdownMenuItem>
					<DropdownMenuItem onClick={onSave}>
						<Download aria-hidden="true" /> Save patch series…
					</DropdownMenuItem>
				</DropdownMenuGroup>
			</DropdownMenuContent>
		</DropdownMenu>
	);
}
