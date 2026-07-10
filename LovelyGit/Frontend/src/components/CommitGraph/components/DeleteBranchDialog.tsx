import { LoaderCircle, ShieldAlert, Trash2 } from "lucide-react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useState } from "react";
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
import { MutationOptionToggle } from "./MutationOptionToggle";

export function DeleteBranchDialog({
	branchName,
	isBusy,
	onConfirm,
	onOpenChange,
}: {
	branchName: string;
	isBusy: boolean;
	onConfirm: (force: boolean) => void;
	onOpenChange: (branchName: string | null) => void;
}) {
	const [force, setForce] = useState(false);
	const reduceMotion = useReducedMotion();

	return (
		<AlertDialog
			onOpenChange={(open) => !open && !isBusy && onOpenChange(null)}
			open
		>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogMedia className="bg-destructive/10 text-destructive">
						<Trash2 aria-hidden="true" />
					</AlertDialogMedia>
					<AlertDialogTitle>Delete local branch {branchName}?</AlertDialogTitle>
					<AlertDialogDescription>
						Safe deletion succeeds only when Git confirms its commits are
						merged. Remote branches are not removed.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<div className="grid gap-3 rounded-lg border bg-card p-3">
					<MutationOptionToggle
						accessibleName="Force delete unmerged branch"
						checked={force}
						icon={
							<ShieldAlert
								aria-hidden="true"
								className="size-4 text-amber-500"
							/>
						}
						id="toggle-force-delete-branch"
						onCheckedChange={setForce}
					>
						Force delete unmerged branch
					</MutationOptionToggle>
					<AnimatePresence initial={false}>
						{force ? (
							<motion.p
								animate={{ height: "auto", opacity: 1, y: 0 }}
								exit={{ height: 0, opacity: 0, y: reduceMotion ? 0 : -4 }}
								initial={{ height: 0, opacity: 0, y: reduceMotion ? 0 : -4 }}
								className="overflow-hidden text-amber-600 text-xs dark:text-amber-400"
							>
								Commits reachable only from this branch may become difficult to
								recover.
							</motion.p>
						) : null}
					</AnimatePresence>
				</div>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isBusy}>Keep branch</AlertDialogCancel>
					<AlertDialogAction
						disabled={isBusy}
						onClick={() => onConfirm(force)}
						variant="destructive"
					>
						{isBusy ? (
							<LoaderCircle aria-hidden="true" className="animate-spin" />
						) : (
							<Trash2 aria-hidden="true" />
						)}
						{isBusy
							? "Deleting"
							: force
								? "Force delete branch"
								: "Delete branch"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
