import { Upload } from "lucide-react";
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

export function PushBranchDialog({
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
	const [isPushing, setIsPushing] = useState(false);
	const canPush = !isPushing && repositoryId !== null;

	const pushBranch = async () => {
		if (!canPush || repositoryId === null) {
			return;
		}

		setIsPushing(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					branchName,
					repositoryId,
				},
				commandType: NativeMessageType.PushBranch,
			});
			toast.success(`Pushed ${branchName}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not push branch",
			);
		} finally {
			setIsPushing(false);
		}
	};

	return (
		<AlertDialog onOpenChange={onOpenChange} open={isOpen}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogTitle className="flex items-center gap-2">
						<Upload aria-hidden="true" />
						Push branch
					</AlertDialogTitle>
					<AlertDialogDescription>
						Push {branchName} to origin. Git will report if the remote rejects
						the update.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isPushing}>Cancel</AlertDialogCancel>
					<AlertDialogAction disabled={!canPush} onClick={pushBranch}>
						{isPushing ? "Pushing" : "Push branch"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
