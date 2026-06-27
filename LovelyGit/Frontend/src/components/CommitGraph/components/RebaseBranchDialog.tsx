import { GitPullRequestArrow } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import {
	AlertDialog,
	AlertDialogAction,
	AlertDialogCancel,
	AlertDialogContent,
	AlertDialogDescription,
	AlertDialogFooter,
	AlertDialogHeader,
	AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function RebaseBranchDialog({
	branchName,
	currentBranchName,
	isOpen,
	onOpenChange,
	onSuccess,
	repositoryId,
}: {
	branchName: string;
	currentBranchName: string | null;
	isOpen: boolean;
	onOpenChange: (isOpen: boolean) => void;
	onSuccess: () => void;
	repositoryId: string | null;
}) {
	const [isRebasing, setIsRebasing] = useState(false);
	const canRebase = !isRebasing && repositoryId !== null;
	const sourceBranchName = currentBranchName ?? "the current branch";

	const rebaseBranch = async () => {
		if (!canRebase || repositoryId === null) {
			return;
		}

		setIsRebasing(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					branchName,
					repositoryId,
				},
				commandType: NativeMessageType.RebaseCurrentBranchOntoBranch,
			});
			toast.success(`Rebased ${sourceBranchName}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not rebase branch",
			);
		} finally {
			setIsRebasing(false);
		}
	};

	return (
		<AlertDialog onOpenChange={onOpenChange} open={isOpen}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogTitle className="flex items-center gap-2">
						<GitPullRequestArrow aria-hidden="true" />
						Rebase current branch
					</AlertDialogTitle>
					<AlertDialogDescription>
						Rebase {sourceBranchName} onto {branchName}. This rewrites commits
						on the current branch. If conflicts occur, Git will leave the
						repository in a rebase state.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isRebasing}>Cancel</AlertDialogCancel>
					<AlertDialogAction disabled={!canRebase} onClick={rebaseBranch}>
						{isRebasing ? "Rebasing" : "Rebase branch"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
