import { GitCommitHorizontal } from "lucide-react";
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

export function CheckoutCommitDetachedDialog({
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
	const [isCheckingOut, setIsCheckingOut] = useState(false);
	const canCheckout = !isCheckingOut && repositoryId !== null;

	const checkoutCommit = async () => {
		if (!canCheckout || repositoryId === null) {
			return;
		}

		setIsCheckingOut(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					commitHash,
					repositoryId,
				},
				commandType: NativeMessageType.CheckoutCommitDetached,
			});
			toast.success(`Checked out ${shortHash(commitHash)}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not checkout commit",
			);
		} finally {
			setIsCheckingOut(false);
		}
	};

	return (
		<AlertDialog onOpenChange={onOpenChange} open={isOpen}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogTitle className="flex items-center gap-2">
						<GitCommitHorizontal aria-hidden="true" />
						Checkout commit
					</AlertDialogTitle>
					<AlertDialogDescription>
						This will detach HEAD at commit {shortHash(commitHash)}. Your
						current branch will stop moving with new commits until you checkout
						a branch.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isCheckingOut}>Cancel</AlertDialogCancel>
					<AlertDialogAction disabled={!canCheckout} onClick={checkoutCommit}>
						{isCheckingOut ? "Checking out" : "Checkout detached"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
