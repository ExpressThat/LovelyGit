import { useState } from "react";
import { BranchPicker } from "@/components/BranchPicker/BranchPicker";
import {
	FolderGit2,
	FolderOpen,
	LoaderCircle,
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
import type { RepositoryWorktreeItem } from "@/generated/types";
import { motion, useReducedMotion } from "@/lib/motion";

export function CreateWorktreeDialog({
	branchName,
	branches,
	existingWorktree,
	isBusy,
	onChooseDestination,
	onBranchChange,
	onClose,
	onCreate,
	onOpenExisting,
}: {
	branchName: string;
	branches: string[];
	existingWorktree: RepositoryWorktreeItem | null;
	isBusy: boolean;
	onChooseDestination: () => Promise<string | null>;
	onBranchChange: (branchName: string) => void;
	onClose: () => void;
	onCreate: (path: string) => void;
	onOpenExisting: (worktree: RepositoryWorktreeItem) => void;
}) {
	const [path, setPath] = useState("");
	const [isChoosing, setIsChoosing] = useState(false);
	const reduceMotion = useReducedMotion();
	const choose = async () => {
		setIsChoosing(true);
		try {
			const selected = await onChooseDestination();
			if (selected) setPath(selected);
		} finally {
			setIsChoosing(false);
		}
	};

	return (
		<Dialog open onOpenChange={(open) => !open && !isBusy && onClose()}>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						if (path.trim()) onCreate(path.trim());
					}}
				>
					<DialogHeader>
						<DialogTitle className="flex items-center gap-2">
							<FolderGit2 aria-hidden="true" className="size-5 text-primary" />
							{branchName ? `Worktree for ${branchName}` : "Create worktree"}
						</DialogTitle>
						<DialogDescription>
							Check out this branch in another folder without switching your
							current workspace.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-4 py-4">
						{!branchName ? (
							<div className="grid gap-2 text-sm">
								<span className="font-medium">Branch</span>
								<BranchPicker
									ariaLabel="Worktree branch"
									disabled={isBusy}
									onValueChange={onBranchChange}
									options={branches}
									placeholder="Choose a local branch"
									value={branchName}
								/>
							</div>
						) : null}
						{existingWorktree ? (
							<motion.div
								animate={{ opacity: 1, y: 0 }}
								className="grid gap-2 rounded-lg border bg-card p-3"
								initial={{ opacity: 0, y: reduceMotion ? 0 : -4 }}
							>
								<span className="text-muted-foreground text-sm">
									This branch is already checked out in a worktree.
								</span>
								<span
									className="truncate font-mono text-xs"
									title={existingWorktree.path}
								>
									{existingWorktree.path}
								</span>
							</motion.div>
						) : branchName ? (
							<label className="grid gap-2 text-sm" htmlFor="worktree-path">
								<span className="font-medium">Empty destination folder</span>
								<span className="flex gap-2">
									<Input
										aria-label="Worktree destination"
										disabled={isBusy || isChoosing}
										id="worktree-path"
										onChange={(event) => setPath(event.currentTarget.value)}
										onInput={(event) => setPath(event.currentTarget.value)}
										placeholder="Choose an empty folder"
										value={path}
									/>
									<Button
										aria-label="Browse for worktree destination"
										disabled={isBusy || isChoosing}
										onClick={() => void choose()}
										type="button"
										variant="outline"
									>
										{isChoosing ? (
											<LoaderCircle className="animate-spin" />
										) : (
											<FolderOpen />
										)}
										Browse
									</Button>
								</span>
							</label>
						) : branches.length === 0 ? (
							<p className="text-sm text-muted-foreground">
								Create a local branch before adding another worktree.
							</p>
						) : null}
					</div>
					<DialogFooter>
						<Button
							disabled={isBusy}
							onClick={onClose}
							type="button"
							variant="outline"
						>
							Cancel
						</Button>
						{existingWorktree ? (
							<Button
								disabled={isBusy || existingWorktree.isCurrent}
								onClick={() => onOpenExisting(existingWorktree)}
								type="button"
							>
								<FolderGit2 /> Open worktree
							</Button>
						) : branchName ? (
							<Button disabled={isBusy || !path.trim()} type="submit">
								{isBusy ? (
									<LoaderCircle className="animate-spin" />
								) : (
									<FolderGit2 />
								)}
								{isBusy ? "Creating" : "Create worktree"}
							</Button>
						) : null}
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
