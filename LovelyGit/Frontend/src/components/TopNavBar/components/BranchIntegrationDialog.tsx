import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { BranchPicker } from "@/components/BranchPicker/BranchPicker";
import {
	GitMerge,
	ListRestart,
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
import type { RepositoryRefItem } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";

export type BranchIntegrationMode = "merge" | "rebase";

export function BranchIntegrationDialog({
	branches,
	currentBranchName,
	mode,
	onOpenWorkingChanges,
	onRepositoryChanged,
	onOpenChange,
	repositoryId,
	targetBranchName = null,
}: {
	branches: RepositoryRefItem[];
	currentBranchName: string | null;
	mode: BranchIntegrationMode | null;
	onOpenWorkingChanges: () => void;
	onRepositoryChanged: () => void;
	onOpenChange: (mode: BranchIntegrationMode | null) => void;
	repositoryId: string | null;
	targetBranchName?: string | null;
}) {
	const [isRunning, setIsRunning] = useState(false);
	const availableBranches = useMemo(
		() =>
			branches
				.filter((branch) => branch.name !== currentBranchName)
				.toSorted((left, right) => left.name.localeCompare(right.name)),
		[branches, currentBranchName],
	);
	const availableBranchNames = useMemo(
		() => availableBranches.map((branch) => branch.name),
		[availableBranches],
	);
	const [selectedBranch, setSelectedBranch] = useState("");

	useEffect(() => {
		if (mode) {
			setSelectedBranch(targetBranchName ?? availableBranches[0]?.name ?? "");
		}
	}, [availableBranches, mode, targetBranchName]);

	if (!mode) {
		return null;
	}

	const isMerge = mode === "merge";
	const operationLabel = isMerge ? "Merge" : "Rebase";
	const hasFixedTarget = targetBranchName !== null;
	const runIntegration = async () => {
		if (!repositoryId || !currentBranchName || !selectedBranch || isRunning) {
			return;
		}

		setIsRunning(true);
		const toastId = toast.loading(
			isMerge
				? `Merging ${selectedBranch} into ${currentBranchName}`
				: `Rebasing ${currentBranchName} onto ${selectedBranch}`,
		);
		try {
			const result = await sendRequestWithResponse(
				{
					arguments: {
						branchName: selectedBranch,
						repositoryId,
					},
					commandType: isMerge
						? NativeMessageType.MergeBranchIntoCurrent
						: NativeMessageType.RebaseCurrentBranchOntoBranch,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			onOpenChange(null);
			onRepositoryChanged();
			if (result.isCompleted) {
				toast.success(
					isMerge
						? `Merged ${selectedBranch} into ${currentBranchName}`
						: `Rebased ${currentBranchName} onto ${selectedBranch}`,
					{ id: toastId },
				);
				return;
			}

			toast.warning(`${operationLabel} paused for conflicts`, {
				description:
					result.message ??
					"Resolve and stage the conflicted files, then continue or abort.",
				id: toastId,
			});
			onOpenWorkingChanges();
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : `${operationLabel} failed.`,
				{ id: toastId },
			);
		} finally {
			setIsRunning(false);
		}
	};

	return (
		<Dialog
			onOpenChange={(open) => {
				if (!open && !isRunning) {
					onOpenChange(null);
				}
			}}
			open
		>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						void runIntegration();
					}}
				>
					<DialogHeader>
						<DialogTitle>
							{hasFixedTarget
								? isMerge
									? `Merge ${selectedBranch} into ${currentBranchName}?`
									: `Rebase ${currentBranchName} onto ${selectedBranch}?`
								: isMerge
									? "Merge into current branch"
									: "Rebase current branch"}
						</DialogTitle>
						<DialogDescription>
							{hasFixedTarget
								? isMerge
									? `${selectedBranch} stays unchanged; ${currentBranchName} receives its commits.`
									: `${currentBranchName} commits will be replayed on top of ${selectedBranch}.`
								: isMerge
									? `Merge another local branch into ${currentBranchName}.`
									: `Replay ${currentBranchName} commits on top of another local branch.`}
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-4 py-4">
						{hasFixedTarget ? (
							<div className="grid grid-cols-[auto_minmax(0,1fr)] items-center gap-x-3 rounded-lg border bg-card px-3 py-2 text-sm">
								<span className="text-muted-foreground">
									{isMerge ? "Branch to merge" : "New base branch"}
								</span>
								<span className="truncate text-right font-mono font-medium">
									{selectedBranch}
								</span>
							</div>
						) : (
							<div className="grid gap-2 text-sm">
								<span className="font-medium">
									{isMerge ? "Branch to merge" : "New base branch"}
								</span>
								<BranchPicker
									ariaLabel={isMerge ? "Branch to merge" : "New base branch"}
									disabled={isRunning}
									onValueChange={setSelectedBranch}
									options={availableBranchNames}
									placeholder="Choose a branch"
									value={selectedBranch}
								/>
							</div>
						)}
						<div className="rounded-lg border bg-muted/40 p-3 text-muted-foreground text-xs">
							{isMerge
								? "The selected branch stays unchanged. Git creates a merge commit only when a fast-forward is not possible."
								: "Rebase rewrites commits on the current branch. Avoid rebasing commits that other people already use."}
						</div>
					</div>
					<DialogFooter className="mx-0 mb-0 px-0 pb-0">
						<Button disabled={!selectedBranch || isRunning} type="submit">
							{isRunning ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : isMerge ? (
								<GitMerge aria-hidden="true" />
							) : (
								<ListRestart aria-hidden="true" />
							)}
							{isRunning ? `${operationLabel} running` : operationLabel}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
