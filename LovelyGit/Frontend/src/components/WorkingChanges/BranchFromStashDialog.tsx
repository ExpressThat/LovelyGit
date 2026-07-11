import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useEffect, useState } from "react";
import {
	GitBranch,
	LoaderCircle,
	PackageOpen,
} from "@/components/icons/lovelyIcons";
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
import type { RepositoryStashItem } from "@/generated/types";

export function BranchFromStashDialog({
	branchNames,
	isBusy,
	onClose,
	onConfirm,
	stash,
}: {
	branchNames: string[];
	isBusy: boolean;
	onClose: () => void;
	onConfirm: (branchName: string) => void;
	stash: RepositoryStashItem | null;
}) {
	const [branchName, setBranchName] = useState("");
	const reduceMotion = useReducedMotion();
	const normalizedName = branchName.trim();
	const duplicate = branchNames.includes(normalizedName);
	const canCreate = normalizedName.length > 0 && !duplicate && !isBusy;

	useEffect(() => {
		if (!stash) setBranchName("");
	}, [stash]);

	return (
		<Dialog
			open={stash !== null}
			onOpenChange={(open) => !open && !isBusy && onClose()}
		>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						if (canCreate) onConfirm(normalizedName);
					}}
				>
					<DialogHeader>
						<DialogTitle>Create branch from {stash?.selector}</DialogTitle>
						<DialogDescription>
							Recover this stash on a branch starting at its original base.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-3 py-4">
						<div className="flex min-w-0 items-center gap-3 rounded-lg border bg-card p-3">
							<PackageOpen aria-hidden="true" className="size-5 text-primary" />
							<div className="min-w-0">
								<p className="truncate font-medium text-sm">
									{stash?.message || "Stashed working changes"}
								</p>
								<p className="font-mono text-muted-foreground text-xs">
									{stash?.commitHash.slice(0, 7)}
								</p>
							</div>
						</div>
						<label className="grid gap-2 text-sm" htmlFor="stash-branch-name">
							<span className="font-medium">Branch name</span>
							<Input
								autoFocus
								id="stash-branch-name"
								onChange={(event) => setBranchName(event.currentTarget.value)}
								onInput={(event) => setBranchName(event.currentTarget.value)}
								placeholder="recover/stashed-work"
								value={branchName}
							/>
						</label>
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
						<p className="text-muted-foreground text-xs">
							The stash is removed only after Git restores it successfully.
						</p>
					</div>
					<DialogFooter className="mx-0 mb-0 px-0 pb-0">
						<Button disabled={!canCreate} type="submit">
							{isBusy ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : (
								<GitBranch aria-hidden="true" />
							)}
							{isBusy ? "Creating branch" : "Create and restore"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
