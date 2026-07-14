import { Trash2 } from "@/components/icons/lovelyIcons";
import {
	AlertDialog,
	AlertDialogAction,
	AlertDialogCancel,
	AlertDialogContent,
	AlertDialogDescription,
	AlertDialogFooter,
	AlertDialogHeader,
	AlertDialogMedia,
	AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { type RepositoryStashItem, StashAction } from "@/generated/types";

export function DropStashDialog({
	busyAction,
	onConfirm,
	onTargetChange,
	target,
}: {
	busyAction: StashAction | null;
	onConfirm: (target: RepositoryStashItem) => void;
	onTargetChange: (target: RepositoryStashItem | null) => void;
	target: RepositoryStashItem | null;
}) {
	const dropping = busyAction === StashAction.Drop;
	return (
		<AlertDialog
			onOpenChange={(isOpen) => {
				if (!isOpen && !dropping) onTargetChange(null);
			}}
			open={target !== null}
		>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogMedia className="bg-destructive/10 text-destructive">
						<Trash2 aria-hidden="true" />
					</AlertDialogMedia>
					<AlertDialogTitle>Delete this stash?</AlertDialogTitle>
					<AlertDialogDescription>
						{target?.selector} will be permanently removed from the repository
						reflog.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={dropping}>Cancel</AlertDialogCancel>
					<AlertDialogAction
						disabled={!target || dropping}
						onClick={() => target && onConfirm(target)}
						variant="destructive"
					>
						Delete stash
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
