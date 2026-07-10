import { Boxes, LoaderCircle, RefreshCw } from "lucide-react";
import { useState } from "react";
import {
	AlertDialog,
	AlertDialogAction,
	AlertDialogCancel,
	AlertDialogContent,
	AlertDialogDescription,
	AlertDialogFooter,
	AlertDialogHeader,
	AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
	DialogTrigger,
} from "@/components/ui/dialog";
import { SubmoduleRow } from "./SubmoduleRow";
import { useSubmoduleManager } from "./useSubmoduleManager";

export function SubmoduleManager({
	repositoryId,
}: {
	repositoryId: string | null;
}) {
	const manager = useSubmoduleManager(repositoryId);
	const [deinitializePath, setDeinitializePath] = useState<string | null>(null);
	return (
		<>
			<Dialog
				onOpenChange={(open) => {
					if (open) void manager.load();
				}}
			>
				<DialogTrigger
					disabled={!repositoryId}
					render={
						<button
							aria-label="Manage submodules"
							className="inline-flex size-9 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-40"
							title="Submodules"
							type="button"
						/>
					}
				>
					<Boxes className="size-5" />
				</DialogTrigger>
				<DialogContent className="max-h-[82vh] overflow-hidden sm:max-w-2xl">
					<DialogHeader>
						<DialogTitle className="flex items-center gap-2">
							<Boxes className="size-5 text-primary" /> Submodules
						</DialogTitle>
						<DialogDescription>
							Initialize and update the nested repositories tracked by this
							repository.
						</DialogDescription>
					</DialogHeader>
					<div className="flex items-center justify-between">
						<span className="text-muted-foreground text-xs">
							{manager.submodules.length} configured
						</span>
						<Button
							disabled={manager.isLoading || manager.busyPath !== null}
							onClick={() => void manager.load()}
							size="sm"
							variant="ghost"
						>
							<RefreshCw className={manager.isLoading ? "animate-spin" : ""} />{" "}
							Refresh
						</Button>
					</div>
					<div className="custom-scrollbar min-h-24 space-y-2 overflow-y-auto">
						{manager.isLoading ? <Loading /> : null}
						{manager.error ? (
							<p className="text-destructive text-sm">{manager.error}</p>
						) : null}
						{!manager.isLoading &&
						!manager.error &&
						manager.submodules.length === 0 ? (
							<p className="py-8 text-center text-muted-foreground text-sm">
								No submodules are configured.
							</p>
						) : null}
						{manager.submodules.map((submodule) => (
							<SubmoduleRow
								busy={manager.busyPath === submodule.path}
								disabled={manager.busyPath !== null}
								key={submodule.path}
								onDeinitialize={() => setDeinitializePath(submodule.path)}
								onRun={(action) => void manager.run(submodule.path, action)}
								submodule={submodule}
							/>
						))}
					</div>
				</DialogContent>
			</Dialog>
			<AlertDialog
				open={deinitializePath !== null}
				onOpenChange={(open) => !open && setDeinitializePath(null)}
			>
				<AlertDialogContent>
					<AlertDialogHeader>
						<AlertDialogTitle>Deinitialize this submodule?</AlertDialogTitle>
						<AlertDialogDescription>
							Git will remove its checked-out files. The recorded submodule
							configuration and commit remain tracked.
						</AlertDialogDescription>
					</AlertDialogHeader>
					<AlertDialogFooter>
						<AlertDialogCancel>Cancel</AlertDialogCancel>
						<AlertDialogAction
							onClick={() => {
								if (deinitializePath)
									void manager.run(deinitializePath, "Deinitialize");
								setDeinitializePath(null);
							}}
							variant="destructive"
						>
							Deinitialize
						</AlertDialogAction>
					</AlertDialogFooter>
				</AlertDialogContent>
			</AlertDialog>
		</>
	);
}

function Loading() {
	return (
		<div className="flex items-center justify-center gap-2 py-8 text-muted-foreground text-sm">
			<LoaderCircle className="animate-spin" /> Reading submodules…
		</div>
	);
}
