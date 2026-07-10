import { LoaderCircle, Plus, RadioTower, Trash2 } from "lucide-react";
import { AnimatePresence } from "motion/react";
import {
	AlertDialog,
	AlertDialogAction,
	AlertDialogCancel,
	AlertDialogContent,
	AlertDialogDescription,
	AlertDialogFooter,
	AlertDialogHeader,
	AlertDialogMedia,
	AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { RemoteEditor } from "./RemoteEditor";
import { RemoteRow } from "./RemoteRow";
import { useRemoteManager } from "./useRemoteManager";

export function RemoteManagerDialog({
	onOpenChange,
	open,
	repositoryId,
}: {
	onOpenChange: (open: boolean) => void;
	open: boolean;
	repositoryId: string | null;
}) {
	const manager = useRemoteManager(repositoryId, open);
	const busy = manager.isLoading || manager.isMutating;
	return (
		<>
			<Dialog
				open={open}
				onOpenChange={(next) => !manager.isMutating && onOpenChange(next)}
			>
				<DialogContent className="overflow-hidden sm:max-w-2xl">
					<DialogHeader>
						<DialogTitle className="flex items-center gap-2">
							<RadioTower aria-hidden="true" className="size-5 text-primary" />
							Manage remotes
						</DialogTitle>
						<DialogDescription>
							Configure where this repository fetches from and pushes to.
						</DialogDescription>
					</DialogHeader>
					{manager.error ? (
						<div className="rounded-md border border-destructive/40 bg-destructive/10 p-3 text-destructive text-sm">
							{manager.error}
						</div>
					) : null}
					<div className="flex items-center justify-between gap-3">
						<span className="text-muted-foreground text-xs">
							{manager.remotes.length}{" "}
							{manager.remotes.length === 1 ? "remote" : "remotes"}
						</span>
						<Button
							disabled={busy || manager.editor !== null}
							onClick={manager.startAdd}
							size="sm"
							type="button"
						>
							<Plus aria-hidden="true" /> Add remote
						</Button>
					</div>
					<AnimatePresence initial={false} mode="popLayout">
						{manager.editor ? (
							<RemoteEditor
								draft={manager.editor}
								existingNames={manager.remotes.map((remote) => remote.name)}
								isSaving={manager.isMutating}
								key={`editor:${manager.editor.originalName ?? "new"}`}
								onCancel={manager.cancelEdit}
								onSave={(draft) => void manager.save(draft)}
							/>
						) : null}
					</AnimatePresence>
					{manager.isLoading ? (
						<div className="flex min-h-32 items-center justify-center gap-2 text-muted-foreground text-sm">
							<LoaderCircle aria-hidden="true" className="animate-spin" />{" "}
							Loading remotes
						</div>
					) : manager.remotes.length === 0 ? (
						<div className="grid min-h-32 place-items-center rounded-lg border border-dashed bg-muted/30 p-6 text-center">
							<div>
								<p className="font-medium text-sm">No remotes configured</p>
								<p className="text-muted-foreground text-xs">
									Add one to fetch, pull, and push.
								</p>
							</div>
						</div>
					) : (
						<ul className="grid min-w-0 max-h-72 gap-2 overflow-y-auto pr-1">
							<AnimatePresence initial={false} mode="popLayout">
								{manager.remotes.map((remote) => (
									<RemoteRow
										disabled={busy || manager.editor !== null}
										key={remote.name}
										onEdit={() => manager.startEdit(remote)}
										onRemove={() => manager.startRemove(remote)}
										remote={remote}
									/>
								))}
							</AnimatePresence>
						</ul>
					)}
				</DialogContent>
			</Dialog>
			<AlertDialog
				open={manager.removeTarget !== null}
				onOpenChange={(next) => !next && manager.closeRemove()}
			>
				<AlertDialogContent>
					<AlertDialogHeader>
						<AlertDialogMedia className="bg-destructive/10 text-destructive">
							<Trash2 aria-hidden="true" />
						</AlertDialogMedia>
						<AlertDialogTitle>
							Remove {manager.removeTarget?.name}?
						</AlertDialogTitle>
						<AlertDialogDescription>
							This removes only the local remote configuration. The hosted
							repository is not deleted.
						</AlertDialogDescription>
					</AlertDialogHeader>
					<AlertDialogFooter>
						<AlertDialogCancel disabled={manager.isMutating}>
							Cancel
						</AlertDialogCancel>
						<AlertDialogAction
							disabled={manager.isMutating}
							onClick={() => void manager.confirmRemove()}
							variant="destructive"
						>
							{manager.isMutating ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : (
								<Trash2 aria-hidden="true" />
							)}
							{manager.isMutating ? "Removing" : "Remove remote"}
						</AlertDialogAction>
					</AlertDialogFooter>
				</AlertDialogContent>
			</AlertDialog>
		</>
	);
}
