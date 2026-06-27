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
import { shortHash } from "../utils/format";

export function CherryPickCommitDialog({
	commitHash,
	isOpen,
	onOpenChange,
	onSuccess,
	repositoryId,
}: {
	commitHash: string;
	isOpen: boolean;
	onOpenChange: (isOpen: boolean) => void;
	onSuccess: () => void;
	repositoryId: string | null;
}) {
	const [isCherryPicking, setIsCherryPicking] = useState(false);
	const canCherryPick = !isCherryPicking && repositoryId !== null;

	const cherryPickCommit = async () => {
		if (!canCherryPick || repositoryId === null) {
			return;
		}

		setIsCherryPicking(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					commitHash,
					repositoryId,
				},
				commandType: NativeMessageType.CherryPickCommit,
			});
			toast.success(`Cherry-picked ${shortHash(commitHash)}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not cherry-pick commit",
			);
		} finally {
			setIsCherryPicking(false);
		}
	};

	return (
		<AlertDialog onOpenChange={onOpenChange} open={isOpen}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogTitle className="flex items-center gap-2">
						<GitPullRequestArrow aria-hidden="true" />
						Cherry-pick commit
					</AlertDialogTitle>
					<AlertDialogDescription>
						Apply commit {shortHash(commitHash)} onto the current branch. If
						conflicts occur, Git will leave the repository in a cherry-pick
						state.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isCherryPicking}>
						Cancel
					</AlertDialogCancel>
					<AlertDialogAction
						disabled={!canCherryPick}
						onClick={cherryPickCommit}
					>
						{isCherryPicking ? "Cherry-picking" : "Cherry-pick"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
