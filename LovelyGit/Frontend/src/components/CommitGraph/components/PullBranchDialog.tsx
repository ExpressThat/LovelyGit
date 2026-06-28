import { RefreshCw } from "lucide-react";
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

export function PullBranchDialog({
	branchName,
	isOpen,
	onOpenChange,
	onSuccess,
	repositoryId,
}: {
	branchName: string;
	isOpen: boolean;
	onOpenChange: (isOpen: boolean) => void;
	onSuccess: () => void;
	repositoryId: string | null;
}) {
	const [isPulling, setIsPulling] = useState(false);
	const canPull = !isPulling && repositoryId !== null;

	const pullBranch = async () => {
		if (!canPull || repositoryId === null) {
			return;
		}

		setIsPulling(true);
		try {
			await sendRequestWithResponse(
				{
					arguments: {
						branchName,
						repositoryId,
					},
					commandType: NativeMessageType.PullBranch,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			toast.success(`Pulled ${branchName}`);
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
			showGitActionError(error, "Could not pull branch");
		} finally {
			setIsPulling(false);
		}
	};

	return (
		<AlertDialog onOpenChange={onOpenChange} open={isOpen}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogTitle className="flex items-center gap-2">
						<RefreshCw aria-hidden="true" />
						Pull branch
					</AlertDialogTitle>
					<AlertDialogDescription>
						Pull origin/{branchName} into the current branch. If conflicts
						occur, Git will leave the repository in a merge state.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isPulling}>Cancel</AlertDialogCancel>
					<AlertDialogAction disabled={!canPull} onClick={pullBranch}>
						{isPulling ? "Pulling" : "Pull branch"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
