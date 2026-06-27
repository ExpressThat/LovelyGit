import { Trash2 } from "lucide-react";
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

export function DeleteBranchDialog({
	branchName,
	force,
	isOpen,
	onOpenChange,
	onSuccess,
	repositoryId,
}: {
	branchName: string;
	force: boolean;
	isOpen: boolean;
	onOpenChange: (isOpen: boolean) => void;
	onSuccess: () => void;
	repositoryId: string | null;
}) {
	const [isDeleting, setIsDeleting] = useState(false);
	const canDelete = !isDeleting && repositoryId !== null;

	const deleteBranch = async () => {
		if (!canDelete || repositoryId === null) {
			return;
		}

		setIsDeleting(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					branchName,
					force,
					repositoryId,
				},
				commandType: NativeMessageType.DeleteBranch,
			});
			toast.success(`Deleted branch ${branchName}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not delete branch",
			);
		} finally {
			setIsDeleting(false);
		}
	};

	return (
		<AlertDialog onOpenChange={onOpenChange} open={isOpen}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogTitle className="flex items-center gap-2">
						<Trash2 aria-hidden="true" />
						Delete branch
					</AlertDialogTitle>
					<AlertDialogDescription>
						{force
							? `Force delete ${branchName}. This removes the branch even if it contains unmerged commits.`
							: `Delete ${branchName}. Git will stop this if the branch has unmerged commits.`}
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isDeleting}>Cancel</AlertDialogCancel>
					<AlertDialogAction
						disabled={!canDelete}
						onClick={deleteBranch}
						variant="destructive"
					>
						{isDeleting ? "Deleting" : force ? "Force delete" : "Delete"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
