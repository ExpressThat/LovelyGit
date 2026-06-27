import { RotateCcw } from "lucide-react";
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

export function RevertCommitDialog({
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
	const [isReverting, setIsReverting] = useState(false);
	const canRevert = !isReverting && repositoryId !== null;

	const revertCommit = async () => {
		if (!canRevert || repositoryId === null) {
			return;
		}

		setIsReverting(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					commitHash,
					repositoryId,
				},
				commandType: NativeMessageType.RevertCommit,
			});
			toast.success(`Reverted ${shortHash(commitHash)}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not revert commit",
			);
		} finally {
			setIsReverting(false);
		}
	};

	return (
		<AlertDialog onOpenChange={onOpenChange} open={isOpen}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogTitle className="flex items-center gap-2">
						<RotateCcw aria-hidden="true" />
						Revert commit
					</AlertDialogTitle>
					<AlertDialogDescription>
						Create a new commit that reverses commit {shortHash(commitHash)}. If
						conflicts occur, Git will leave the repository in a revert state.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isReverting}>Cancel</AlertDialogCancel>
					<AlertDialogAction disabled={!canRevert} onClick={revertCommit}>
						{isReverting ? "Reverting" : "Revert"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
