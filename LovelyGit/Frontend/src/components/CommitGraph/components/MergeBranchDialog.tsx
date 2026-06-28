import { GitPullRequestArrow } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import {
	showConflictWorkspaceIfNeeded,
	showGitActionError,
} from "@/components/Conflicts/ConflictTransition";
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
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function MergeBranchDialog({
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
	const [isMerging, setIsMerging] = useState(false);
	const canMerge = !isMerging && repositoryId !== null;
	const targetBranchName = currentBranchName ?? "the current branch";

	const mergeBranch = async () => {
		if (!canMerge || repositoryId === null) {
			return;
		}

		setIsMerging(true);
		try {
			await sendRequestWithResponse(
				{
					arguments: {
						branchName,
						repositoryId,
					},
					commandType: NativeMessageType.MergeBranchIntoCurrent,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			toast.success(`Merged ${branchName}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			if (
				await showConflictWorkspaceIfNeeded({
					repositoryId,
				})
			) {
				onSuccess();
				onOpenChange(false);
				return;
			}
			showGitActionError(error, "Could not merge branch");
		} finally {
			setIsMerging(false);
		}
	};

	return (
		<AlertDialog onOpenChange={onOpenChange} open={isOpen}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogTitle className="flex items-center gap-2">
						<GitPullRequestArrow aria-hidden="true" />
						Merge branch
					</AlertDialogTitle>
					<AlertDialogDescription>
						Merge {branchName} into {targetBranchName}. If conflicts occur, Git
						will leave the repository in a merge state.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isMerging}>Cancel</AlertDialogCancel>
					<AlertDialogAction disabled={!canMerge} onClick={mergeBranch}>
						{isMerging ? "Merging" : "Merge branch"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
