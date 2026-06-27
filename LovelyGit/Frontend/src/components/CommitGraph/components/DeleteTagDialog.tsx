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

export function DeleteTagDialog({
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
	const [isDeleting, setIsDeleting] = useState(false);
	const canDelete = !isDeleting && repositoryId !== null;

	const deleteTag = async () => {
		if (!canDelete || repositoryId === null) {
			return;
		}

		setIsDeleting(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					repositoryId,
					tagName,
				},
				commandType: NativeMessageType.DeleteTag,
			});
			toast.success(`Deleted tag ${tagName}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not delete tag",
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
						Delete local tag
					</AlertDialogTitle>
					<AlertDialogDescription>
						Delete local tag {tagName}. This does not delete any matching remote
						tag.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isDeleting}>Cancel</AlertDialogCancel>
					<AlertDialogAction
						disabled={!canDelete}
						onClick={deleteTag}
						variant="destructive"
					>
						{isDeleting ? "Deleting" : "Delete tag"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
