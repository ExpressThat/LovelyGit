import { useState } from "react";
import { DeferredPrimaryOverlay } from "@/AppPrimaryOverlays";
import {
	ChevronDown,
	ShieldAlert,
	Upload,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuItem,
	DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { GitPushMode } from "@/generated/types";
import { createDeferredLoader } from "@/lib/deferredLoader";
import { SyncCountBadge, syncActionLabel } from "./SyncCountBadge";

const forcePushDialogLoader = createDeferredLoader(() =>
	import("./ForcePushDialog").then((module) => module.ForcePushDialog),
);

export function PushActionsControl({
	canRun,
	currentBranchName,
	isBusy,
	isHistoryPartial,
	onPush,
	outgoingCount,
}: {
	canRun: boolean;
	currentBranchName: string | null;
	isBusy: boolean;
	isHistoryPartial: boolean;
	onPush: (mode: GitPushMode) => Promise<boolean>;
	outgoingCount: number;
}) {
	const [menuOpen, setMenuOpen] = useState(false);
	const [forcePushOpen, setForcePushOpen] = useState(false);
	return (
		<>
			<div className="inline-flex h-9 overflow-hidden rounded-md border bg-background">
				<Button
					aria-label={syncActionLabel(
						"Push",
						outgoingCount,
						"outgoing",
						isHistoryPartial,
					)}
					className="h-full rounded-none border-0 px-3"
					disabled={!canRun}
					onClick={() => void onPush("Normal")}
					size="sm"
					title={syncActionLabel(
						"Push",
						outgoingCount,
						"outgoing",
						isHistoryPartial,
					)}
					type="button"
					variant="ghost"
				>
					<Upload
						aria-hidden="true"
						className={`size-6 ${isBusy ? "animate-pulse" : ""}`}
					/>
					<span>Push</span>
					<SyncCountBadge
						count={outgoingCount}
						direction="outgoing"
						isPartial={isHistoryPartial}
					/>
				</Button>
				<DropdownMenu open={menuOpen} onOpenChange={setMenuOpen}>
					<DropdownMenuTrigger
						aria-label="More push actions"
						className="inline-flex h-full w-8 items-center justify-center border-l text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50"
						disabled={!canRun}
						title="More push actions"
					>
						<ChevronDown aria-hidden="true" className="size-4" />
					</DropdownMenuTrigger>
					<DropdownMenuContent align="end" className="min-w-64 p-1">
						<DropdownMenuItem
							className="min-h-9"
							onClick={() => {
								setMenuOpen(false);
								setForcePushOpen(true);
							}}
							variant="destructive"
						>
							<ShieldAlert aria-hidden="true" />
							<div>
								<div className="font-medium">Force push with lease…</div>
								<div className="text-[10px] opacity-75">
									Safely rewrite remote history
								</div>
							</div>
						</DropdownMenuItem>
					</DropdownMenuContent>
				</DropdownMenu>
			</div>
			{forcePushOpen ? (
				<DeferredPrimaryOverlay
					fallback={null}
					loader={forcePushDialogLoader}
					props={{
						branchName: currentBranchName,
						isBusy,
						onConfirm: () => {
							void onPush("ForceWithLease").then((completed) => {
								if (completed) setForcePushOpen(false);
							});
						},
						onOpenChange: (open) => {
							if (!isBusy) setForcePushOpen(open);
						},
						open: true,
					}}
				/>
			) : null}
		</>
	);
}
