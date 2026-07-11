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
import type { WorkingTreeChangedFile } from "@/generated/types";

const previewLimit = 5;

export function DiscardWorkingTreeChangesDialog({
	files,
	isDiscarding,
	isOpen,
	onConfirm,
	onOpenChange,
}: {
	files: WorkingTreeChangedFile[];
	isDiscarding: boolean;
	isOpen: boolean;
	onConfirm: () => void;
	onOpenChange: (isOpen: boolean) => void;
}) {
	const remainingCount = Math.max(0, files.length - previewLimit);

	return (
		<AlertDialog onOpenChange={onOpenChange} open={isOpen}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogMedia className="bg-destructive/10 text-destructive">
						<Trash2 aria-hidden="true" />
					</AlertDialogMedia>
					<AlertDialogTitle>Discard selected changes?</AlertDialogTitle>
					<AlertDialogDescription>
						This will permanently restore tracked files and delete untracked
						files from the working tree.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<ul className="max-h-36 space-y-1 overflow-auto rounded-md border bg-card p-2 text-xs">
					{files.slice(0, previewLimit).map((file) => (
						<li
							className="truncate font-mono"
							key={`${file.group}:${file.path}`}
						>
							{file.path}
						</li>
					))}
					{remainingCount > 0 ? (
						<li className="text-muted-foreground">
							...and {remainingCount} more
						</li>
					) : null}
				</ul>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isDiscarding}>Cancel</AlertDialogCancel>
					<AlertDialogAction
						disabled={isDiscarding || files.length === 0}
						onClick={onConfirm}
						variant="destructive"
					>
						{isDiscarding ? "Discarding" : "Discard changes"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
