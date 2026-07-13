import { useState } from "react";
import { toast } from "sonner";
import {
	GitCommitHorizontal,
	LoaderCircle,
	Undo2,
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
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { shortHash } from "../utils/format";

type CommitOperationMode = "cherry-pick" | "revert";

export function CommitOperationDialog({
	commits,
	currentBranchName,
	mode,
	onOpenChange,
	onOpenWorkingChanges,
	onRepositoryChanged,
	repositoryId,
}: {
	commits: CommitGraphRow[] | null;
	currentBranchName: string | null;
	mode: CommitOperationMode;
	onOpenChange: (commits: CommitGraphRow[] | null) => void;
	onOpenWorkingChanges: () => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
}) {
	const [isRunning, setIsRunning] = useState(false);
	if (!commits?.length) return null;
	const count = commits.length;
	const targetLabel =
		count === 1 ? shortHash(commits[0].commit.hash) : `${count} commits`;
	const isRevert = mode === "revert";
	const action = isRevert ? "Revert" : "Cherry-pick";
	const runningAction = isRevert ? "Reverting" : "Cherry-picking";

	const runOperation = async () => {
		if (!repositoryId || !currentBranchName || isRunning) return;
		setIsRunning(true);
		const toastId = toast.loading(
			`${runningAction} ${targetLabel} on ${currentBranchName}`,
		);
		try {
			const response = await sendRequestWithResponse(
				{
					arguments: {
						commitHashes: commits.map((commit) => commit.commit.hash),
						repositoryId,
					},
					commandType: isRevert
						? NativeMessageType.RevertCommit
						: NativeMessageType.CherryPickCommit,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			onOpenChange(null);
			onRepositoryChanged();
			if (response.isCompleted) {
				toast.success(`${action} completed on ${currentBranchName}`, {
					id: toastId,
				});
				return;
			}
			toast.warning(`${action} paused for conflicts`, {
				description:
					response.message ??
					"Resolve and stage the conflicted files, then continue or abort.",
				id: toastId,
			});
			onOpenWorkingChanges();
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : `${action} failed.`,
				{ id: toastId },
			);
		} finally {
			setIsRunning(false);
		}
	};

	return (
		<Dialog
			onOpenChange={(open) => !open && !isRunning && onOpenChange(null)}
			open
		>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						void runOperation();
					}}
				>
					<DialogHeader>
						<DialogTitle>
							{action} {targetLabel} on {currentBranchName}?
						</DialogTitle>
						<DialogDescription>
							{isRevert
								? `Create ${count === 1 ? "a new commit" : `${count} new commits`} on ${currentBranchName} in newest-to-oldest order.`
								: `Apply ${count === 1 ? "this commit" : `these ${count} commits`} on ${currentBranchName} in oldest-to-newest order.`}
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-3 py-4">
						<div className="custom-scrollbar max-h-52 overflow-y-auto rounded-lg border bg-card">
							{commits.map((commit, index) => (
								<div
									className="grid grid-cols-[2rem_1fr_auto] items-center gap-2 border-b px-3 py-2 last:border-b-0"
									key={commit.commit.hash}
								>
									<span className="text-center text-muted-foreground text-xs">
										{index + 1}
									</span>
									<span className="truncate font-medium text-sm">
										{commit.commit.message.split(/\r?\n/, 1)[0] ||
											"(no commit message)"}
									</span>
									<span className="font-mono text-muted-foreground text-xs">
										{shortHash(commit.commit.hash)}
									</span>
								</div>
							))}
						</div>
						<p className="rounded-lg border bg-muted/40 p-3 text-muted-foreground text-xs">
							Git keeps the existing history unchanged. If changes overlap, the
							operation pauses so you can resolve conflicts and continue or
							abort.
						</p>
					</div>
					<DialogFooter className="mx-0 mb-0 px-0 pb-0">
						<Button disabled={isRunning} type="submit">
							{isRunning ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : isRevert ? (
								<Undo2 aria-hidden="true" />
							) : (
								<GitCommitHorizontal aria-hidden="true" />
							)}
							{isRunning ? `${action} running` : action}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
