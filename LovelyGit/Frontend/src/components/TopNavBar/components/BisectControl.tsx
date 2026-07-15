import { useState } from "react";
import { DeferredPrimaryOverlay } from "@/AppPrimaryOverlays";
import { SearchCode } from "@/components/icons/lovelyIcons";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
	DialogTrigger,
} from "@/components/ui/dialog";
import { createDeferredLoader } from "@/lib/deferredLoader";
import { motion } from "@/lib/motion";
import { useBisectSession } from "./useBisectSession";

const bisectSessionLoader = createDeferredLoader(() =>
	import("./BisectSessionContent").then(
		(module) => module.BisectSessionContent,
	),
);

export function BisectControl({
	repositoryId,
}: {
	repositoryId: string | null;
}) {
	const [open, setOpen] = useState(false);
	const session = useBisectSession(repositoryId);
	return (
		<Dialog
			onOpenChange={(nextOpen) => {
				setOpen(nextOpen);
				if (nextOpen) void session.load();
			}}
			open={open}
		>
			<DialogTrigger
				disabled={!repositoryId}
				render={
					<button
						aria-label="Manage bisect"
						className="relative inline-flex size-9 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-40"
						title="Git bisect"
						type="button"
					/>
				}
			>
				<SearchCode aria-hidden="true" className="size-5" />
				{session.state?.isActive ? (
					<motion.span
						animate={{ scale: 1 }}
						className="absolute right-1 top-1 size-2 rounded-full bg-primary ring-2 ring-card"
						initial={{ scale: 0 }}
						layoutId="active-bisect-indicator"
					/>
				) : null}
			</DialogTrigger>
			<DialogContent className="overflow-hidden sm:max-w-xl">
				<DialogHeader>
					<DialogTitle className="flex items-center gap-2">
						<SearchCode className="size-5 text-primary" /> Git bisect
					</DialogTitle>
					<DialogDescription>
						Narrow down the exact commit that introduced a regression.
					</DialogDescription>
				</DialogHeader>
				{open ? (
					<DeferredPrimaryOverlay
						fallback={
							<div className="h-32 animate-pulse rounded-lg bg-muted" />
						}
						loader={bisectSessionLoader}
						props={{
							busyAction: session.busyAction,
							isLoading: session.isLoading,
							onRun: (action) => void session.run(action),
							state: session.state,
						}}
					/>
				) : null}
			</DialogContent>
		</Dialog>
	);
}
