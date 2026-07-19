import { useState } from "react";
import {
	CloudDownload,
	LoaderCircle,
	Trash2,
} from "@/components/icons/lovelyIcons";
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
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { AnimatePresence, motion, useReducedMotion } from "@/lib/motion";

export function CheckoutRemoteBranchDialog({
	existingBranchNames,
	isBusy,
	onConfirm,
	onOpenChange,
	remoteBranchName,
}: {
	existingBranchNames: string[];
	isBusy: boolean;
	onConfirm: (localBranchName: string) => void;
	onOpenChange: (branchName: string | null) => void;
	remoteBranchName: string;
}) {
	const [localBranchName, setLocalBranchName] = useState(
		localNameFor(remoteBranchName),
	);
	const reduceMotion = useReducedMotion();
	const normalizedName = localBranchName.trim();
	const duplicate = existingBranchNames.includes(normalizedName);
	const canCheckout = normalizedName.length > 0 && !duplicate && !isBusy;
	return (
		<Dialog
			onOpenChange={(open) => !open && !isBusy && onOpenChange(null)}
			open
		>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						if (canCheckout) onConfirm(normalizedName);
					}}
				>
					<DialogHeader>
						<DialogTitle>Check out {remoteBranchName}</DialogTitle>
						<DialogDescription>
							Create and switch to a local branch that tracks this remote
							branch.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-2 py-4">
						<label className="font-medium text-sm" htmlFor="remote-local-name">
							Local branch name
						</label>
						<Input
							autoFocus
							id="remote-local-name"
							onChange={(event) =>
								setLocalBranchName(event.currentTarget.value)
							}
							onInput={(event) => setLocalBranchName(event.currentTarget.value)}
							value={localBranchName}
						/>
						<AnimatePresence initial={false}>
							{duplicate ? (
								<motion.p
									animate={{ opacity: 1, y: 0 }}
									className="text-destructive text-xs"
									exit={{ opacity: 0, y: reduceMotion ? 0 : -3 }}
									initial={{ opacity: 0, y: reduceMotion ? 0 : -3 }}
								>
									A local branch with this name already exists.
								</motion.p>
							) : null}
						</AnimatePresence>
					</div>
					<DialogFooter className="mx-0 mb-0 px-0 pb-0">
						<Button disabled={!canCheckout} type="submit">
							{isBusy ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : (
								<CloudDownload aria-hidden="true" />
							)}
							{isBusy ? "Checking out" : "Create tracking branch"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}

export function DeleteRemoteBranchDialog({
	isBusy,
	onConfirm,
	onOpenChange,
	remoteBranchName,
}: {
	isBusy: boolean;
	onConfirm: () => void;
	onOpenChange: (branchName: string | null) => void;
	remoteBranchName: string;
}) {
	return (
		<AlertDialog
			onOpenChange={(open) => !open && !isBusy && onOpenChange(null)}
			open
		>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogMedia className="bg-destructive/10 text-destructive">
						<Trash2 aria-hidden="true" />
					</AlertDialogMedia>
					<AlertDialogTitle>Delete {remoteBranchName}?</AlertDialogTitle>
					<AlertDialogDescription>
						This removes the branch from the shared remote for every
						collaborator. Your local branches and commits are not deleted.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isBusy}>
						Keep remote branch
					</AlertDialogCancel>
					<AlertDialogAction
						disabled={isBusy}
						onClick={onConfirm}
						variant="destructive"
					>
						{isBusy ? (
							<LoaderCircle aria-hidden="true" className="animate-spin" />
						) : (
							<Trash2 aria-hidden="true" />
						)}
						{isBusy ? "Deleting" : "Delete remote branch"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}

function localNameFor(remoteBranchName: string) {
	const slashIndex = remoteBranchName.indexOf("/");
	return slashIndex >= 0
		? remoteBranchName.slice(slashIndex + 1)
		: remoteBranchName;
}
