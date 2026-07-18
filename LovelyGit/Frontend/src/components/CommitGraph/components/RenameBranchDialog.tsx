import { useState } from "react";
import { GitBranch, LoaderCircle } from "@/components/icons/lovelyIcons";
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

export function RenameBranchDialog({
	branchName,
	existingBranchNames,
	isBusy,
	onConfirm,
	onOpenChange,
}: {
	branchName: string;
	existingBranchNames: string[];
	isBusy: boolean;
	onConfirm: (newBranchName: string) => void;
	onOpenChange: (branchName: string | null) => void;
}) {
	const [newBranchName, setNewBranchName] = useState(branchName);
	const reduceMotion = useReducedMotion();
	const normalizedName = newBranchName.trim();
	const unchanged = normalizedName === branchName;
	const duplicate = !unchanged && existingBranchNames.includes(normalizedName);
	const canRename = normalizedName.length > 0 && !unchanged && !duplicate;

	return (
		<Dialog
			onOpenChange={(open) => !open && !isBusy && onOpenChange(null)}
			open
		>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						if (canRename) onConfirm(normalizedName);
					}}
				>
					<DialogHeader>
						<DialogTitle>Rename {branchName}</DialogTitle>
						<DialogDescription>
							Update this local branch name without changing its commits.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-2 py-4">
						<label
							className="font-medium text-sm"
							htmlFor="renamed-branch-name"
						>
							New branch name
						</label>
						<Input
							aria-label="New branch name"
							autoFocus
							id="renamed-branch-name"
							onChange={(event) => setNewBranchName(event.currentTarget.value)}
							onInput={(event) => setNewBranchName(event.currentTarget.value)}
							value={newBranchName}
						/>
						<AnimatePresence initial={false}>
							{duplicate ? (
								<motion.p
									animate={{ opacity: 1, y: 0 }}
									exit={{ opacity: 0, y: reduceMotion ? 0 : -3 }}
									initial={{ opacity: 0, y: reduceMotion ? 0 : -3 }}
									className="text-destructive text-xs"
								>
									A local branch with this name already exists.
								</motion.p>
							) : null}
						</AnimatePresence>
					</div>
					<DialogFooter className="mx-0 mb-0 px-0 pb-0">
						<Button disabled={!canRename || isBusy} type="submit">
							{isBusy ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : (
								<GitBranch aria-hidden="true" />
							)}
							{isBusy ? "Renaming" : "Rename branch"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
