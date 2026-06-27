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

export function CheckoutTagDialog({
	isOpen,
	onOpenChange,
	onSuccess,
	repositoryId,
	tagName,
}: {
	isOpen: boolean;
	onOpenChange: (isOpen: boolean) => void;
	onSuccess: () => void;
	repositoryId: string | null;
	tagName: string;
}) {
	const [isCheckingOut, setIsCheckingOut] = useState(false);
	const canCheckout = !isCheckingOut && repositoryId !== null;

	const checkoutTag = async () => {
		if (!canCheckout || repositoryId === null) {
			return;
		}

		setIsCheckingOut(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					repositoryId,
					tagName,
				},
				commandType: NativeMessageType.CheckoutTag,
			});
			toast.success(`Checked out ${tagName}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not checkout tag",
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
						Checkout tag
					</AlertDialogTitle>
					<AlertDialogDescription>
						This will detach HEAD at tag {tagName}. Your current branch will
						stop moving with new commits until you checkout a branch.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isCheckingOut}>Cancel</AlertDialogCancel>
					<AlertDialogAction disabled={!canCheckout} onClick={checkoutTag}>
						{isCheckingOut ? "Checking out" : "Checkout detached"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
