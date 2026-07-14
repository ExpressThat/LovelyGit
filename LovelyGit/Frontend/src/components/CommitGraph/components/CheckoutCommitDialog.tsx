import { useState } from "react";
import { toast } from "sonner";
import {
	GitCommitHorizontal,
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
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { motion, useReducedMotion } from "@/lib/motion";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { shortHash } from "../utils/format";

export function CheckoutCommitDialog({
	commit,
	onClose,
	onRepositoryChanged,
	repositoryId,
}: {
	commit: CommitGraphRow;
	onClose: () => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
}) {
	const [isRunning, setIsRunning] = useState(false);
	const reduceMotion = useReducedMotion();
	const hash = shortHash(commit.commit.hash);
	const subject = commit.commit.message.split(/\r?\n/, 1)[0] || "(no message)";
	const checkout = async () => {
		if (!repositoryId || isRunning) return;
		setIsRunning(true);
		const toastId = toast.loading(`Checking out ${hash}`);
		try {
			await sendRequestWithResponse(
				{
					arguments: { commitHash: commit.commit.hash, repositoryId },
					commandType: NativeMessageType.CheckoutCommit,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			onRepositoryChanged();
			onClose();
			toast.success(`Checked out ${hash} in detached HEAD mode`, {
				id: toastId,
			});
		} catch (error) {
			toast.error(error instanceof Error ? error.message : "Checkout failed.", {
				id: toastId,
			});
		} finally {
			setIsRunning(false);
		}
	};
	return (
		<Dialog onOpenChange={(open) => !open && !isRunning && onClose()} open>
			<DialogContent>
				<DialogHeader>
					<DialogTitle>Checkout {hash}?</DialogTitle>
					<DialogDescription>
						Inspect this exact snapshot without moving a branch.
					</DialogDescription>
				</DialogHeader>
				<motion.div
					animate={{ opacity: 1, y: 0 }}
					className="my-4 grid gap-3 rounded-lg border bg-card p-4"
					initial={{ opacity: 0, y: reduceMotion ? 0 : 4 }}
				>
					<div className="flex items-center gap-2">
						<GitCommitHorizontal
							aria-hidden="true"
							className="size-5 text-primary"
						/>
						<code className="font-semibold text-sm">{hash}</code>
					</div>
					<p className="truncate text-sm">{subject}</p>
					<p className="text-muted-foreground text-xs">
						HEAD becomes detached. Your branches remain unchanged, and you can
						create a branch from this commit whenever you want to keep new work.
					</p>
				</motion.div>
				<DialogFooter className="mx-0 mb-0 px-0 pb-0">
					<Button
						disabled={!repositoryId || isRunning}
						onClick={() => void checkout()}
						type="button"
					>
						{isRunning ? (
							<LoaderCircle aria-hidden="true" className="animate-spin" />
						) : (
							<GitCommitHorizontal aria-hidden="true" />
						)}
						{isRunning ? "Checking out" : "Checkout detached"}
					</Button>
				</DialogFooter>
			</DialogContent>
		</Dialog>
	);
}
