import { useState } from "react";
import { toast } from "sonner";
import { SearchCode } from "@/components/icons/lovelyIcons";
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
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { shortHash } from "../utils/format";

export function StartBisectDialog({
	commit,
	onOpenChange,
	repositoryId,
}: {
	commit: CommitGraphRow | null;
	onOpenChange: (open: boolean) => void;
	repositoryId: string | null;
}) {
	const [isStarting, setIsStarting] = useState(false);
	const start = async () => {
		if (!commit || !repositoryId || isStarting) return;
		setIsStarting(true);
		const toastId = toast.loading("Starting bisect session…");
		try {
			await sendRequestWithResponse(
				{
					commandType: "ManageBisect",
					arguments: {
						action: "Start",
						goodCommit: commit.commit.hash,
						repositoryId,
					},
				},
				{ timeoutMs: 30_000 },
			);
			toast.success("Bisect started at the midpoint", { id: toastId });
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Git could not start bisect.",
				{ id: toastId },
			);
		} finally {
			setIsStarting(false);
		}
	};
	const subject = commit?.commit.message;
	return (
		<AlertDialog
			onOpenChange={(open) => !isStarting && onOpenChange(open)}
			open={commit !== null}
		>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogMedia className="bg-primary/10 text-primary">
						<SearchCode />
					</AlertDialogMedia>
					<AlertDialogTitle>Start a bisect session?</AlertDialogTitle>
					<AlertDialogDescription>
						LovelyGit will mark the current HEAD as bad and{" "}
						<span className="font-mono text-foreground">
							{commit ? shortHash(commit.commit.hash) : "this commit"}
						</span>{" "}
						as good, then check out the first midpoint to test.
					</AlertDialogDescription>
				</AlertDialogHeader>
				{subject ? (
					<div className="rounded-md border bg-card p-3">
						<p className="text-muted-foreground text-xs">Known-good revision</p>
						<p className="mt-1 font-medium text-sm">{subject}</p>
					</div>
				) : null}
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isStarting}>Cancel</AlertDialogCancel>
					<AlertDialogAction
						disabled={isStarting || !repositoryId}
						onClick={(event) => {
							event.preventDefault();
							void start();
						}}
					>
						Start bisect
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
