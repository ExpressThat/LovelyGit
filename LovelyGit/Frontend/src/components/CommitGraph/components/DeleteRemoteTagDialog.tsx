import { Cloud, LoaderCircle, Trash2 } from "@/components/icons/lovelyIcons";
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

export function DeleteRemoteTagDialog({
	isBusy,
	onConfirm,
	onOpenChange,
	remoteName,
	tagName,
}: {
	isBusy: boolean;
	onConfirm: () => void;
	onOpenChange: (tagName: string | null) => void;
	remoteName: string | null;
	tagName: string | null;
}) {
	return (
		<AlertDialog
			onOpenChange={(open) => !open && !isBusy && onOpenChange(null)}
			open={tagName !== null}
		>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogMedia className="bg-destructive/10 text-destructive">
						<Cloud aria-hidden="true" />
					</AlertDialogMedia>
					<AlertDialogTitle>
						Delete {tagName} from {remoteName ?? "remote"}?
					</AlertDialogTitle>
					<AlertDialogDescription>
						This deletes the remote tag for everyone. Your local tag remains
						available and can be pushed again later.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isBusy}>
						Keep remote tag
					</AlertDialogCancel>
					<AlertDialogAction
						disabled={isBusy || !remoteName}
						onClick={onConfirm}
						variant="destructive"
					>
						{isBusy ? (
							<LoaderCircle aria-hidden="true" className="animate-spin" />
						) : (
							<Trash2 aria-hidden="true" />
						)}
						{isBusy ? "Deleting" : "Delete remote tag"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
