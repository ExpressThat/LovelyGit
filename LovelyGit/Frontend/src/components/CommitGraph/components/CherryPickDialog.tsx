import { GitCommitHorizontal, LoaderCircle } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
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

export function CherryPickDialog({
	commit,
	currentBranchName,
	onOpenChange,
	onOpenWorkingChanges,
	onRepositoryChanged,
	repositoryId,
}: {
	commit: CommitGraphRow | null;
	currentBranchName: string | null;
	onOpenChange: (commit: CommitGraphRow | null) => void;
	onOpenWorkingChanges: () => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
}) {
	const [isRunning, setIsRunning] = useState(false);
	if (!commit) {
		return null;
	}

	const abbreviatedHash = shortHash(commit.commit.hash);
	const subject =
		commit.commit.message.split(/\r?\n/, 1)[0] || "(no commit message)";
	const cherryPick = async () => {
		if (!repositoryId || !currentBranchName || isRunning) {
			return;
		}

		setIsRunning(true);
		const toastId = toast.loading(
			`Cherry-picking ${abbreviatedHash} onto ${currentBranchName}`,
		);
		try {
			const response = await sendRequestWithResponse(
				{
					arguments: { commitHash: commit.commit.hash, repositoryId },
					commandType: NativeMessageType.CherryPickCommit,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			onOpenChange(null);
			onRepositoryChanged();
			if (response.isCompleted) {
				toast.success(
					`Cherry-picked ${abbreviatedHash} onto ${currentBranchName}`,
					{
						id: toastId,
					},
				);
				return;
			}

			toast.warning("Cherry-pick paused for conflicts", {
				description:
					response.message ??
					"Resolve and stage the conflicted files, then continue or abort.",
				id: toastId,
			});
			onOpenWorkingChanges();
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Cherry-pick failed.",
				{ id: toastId },
			);
		} finally {
			setIsRunning(false);
		}
	};

	return (
		<Dialog
			onOpenChange={(open) => {
				if (!open && !isRunning) onOpenChange(null);
			}}
			open
		>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						void cherryPick();
					}}
				>
					<DialogHeader>
						<DialogTitle>
							Cherry-pick {abbreviatedHash} onto {currentBranchName}?
						</DialogTitle>
						<DialogDescription>
							Apply this commit as a new commit on {currentBranchName}.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-3 py-4">
						<div className="grid gap-1 rounded-lg border bg-card px-3 py-2">
							<span className="truncate font-medium text-sm">{subject}</span>
							<span className="font-mono text-muted-foreground text-xs">
								{commit.commit.hash}
							</span>
						</div>
						<p className="rounded-lg border bg-muted/40 p-3 text-muted-foreground text-xs">
							The source commit stays unchanged. If changes overlap, Git pauses
							so you can resolve conflicts and continue or abort.
						</p>
					</div>
					<DialogFooter className="mx-0 mb-0 px-0 pb-0">
						<Button disabled={isRunning} type="submit">
							{isRunning ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : (
								<GitCommitHorizontal aria-hidden="true" />
							)}
							{isRunning ? "Cherry-pick running" : "Cherry-pick"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
