import { LoaderCircle, Trash2 } from "lucide-react";
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

export function DeleteTagDialog({
	isBusy,
	onConfirm,
	onOpenChange,
	tagName,
}: {
	isBusy: boolean;
	onConfirm: () => void;
	onOpenChange: (tagName: string | null) => void;
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
						<Trash2 aria-hidden="true" />
					</AlertDialogMedia>
					<AlertDialogTitle>Delete local tag {tagName}?</AlertDialogTitle>
					<AlertDialogDescription>
						This removes only the local tag. Any copy already pushed to a remote
						remains there.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isBusy}>Keep tag</AlertDialogCancel>
					<AlertDialogAction
						disabled={isBusy}
						onClick={onConfirm}
						variant="destructive"
					>
						{isBusy ? (
							<LoaderCircle aria-hidden="true" className="animate-spin" />
						) : (
							<Trash2 aria-hidden="true" />
						)}
						{isBusy ? "Deleting" : "Delete local tag"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
