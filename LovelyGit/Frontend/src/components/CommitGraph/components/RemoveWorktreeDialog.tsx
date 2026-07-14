import { useState } from "react";
import {
	LoaderCircle,
	ShieldAlert,
	Trash2,
} from "@/components/icons/lovelyIcons";
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
import type { RepositoryWorktreeItem } from "@/generated/types";
import { AnimatePresence, motion, useReducedMotion } from "@/lib/motion";
import { MutationOptionToggle } from "./MutationOptionToggle";

export function RemoveWorktreeDialog({
	isBusy,
	onClose,
	onConfirm,
	worktree,
}: {
	isBusy: boolean;
	onClose: () => void;
	onConfirm: (force: boolean) => void;
	worktree: RepositoryWorktreeItem;
}) {
	const [force, setForce] = useState(false);
	const reduceMotion = useReducedMotion();
	return (
		<AlertDialog open onOpenChange={(open) => !open && !isBusy && onClose()}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogMedia className="bg-destructive/10 text-destructive">
						<Trash2 aria-hidden="true" />
					</AlertDialogMedia>
					<AlertDialogTitle>
						Remove {worktree.branchName ?? "detached"} worktree?
					</AlertDialogTitle>
					<AlertDialogDescription>
						Git removes the linked folder and its administrative metadata. The
						branch and its commits remain available.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<div className="grid min-w-0 gap-3 rounded-lg border bg-card p-3">
					<span
						className="block min-w-0 truncate font-mono text-xs"
						title={worktree.path}
					>
						{worktree.path}
					</span>
					<MutationOptionToggle
						accessibleName="Force remove worktree with changes"
						checked={force}
						icon={
							<ShieldAlert
								aria-hidden="true"
								className="size-4 text-amber-500"
							/>
						}
						id="toggle-force-remove-worktree"
						onCheckedChange={setForce}
					>
						Force removal if files have changes
					</MutationOptionToggle>
					<AnimatePresence initial={false}>
						{force ? (
							<motion.p
								animate={{ height: "auto", opacity: 1, y: 0 }}
								className="overflow-hidden text-amber-600 text-xs dark:text-amber-400"
								exit={{ height: 0, opacity: 0, y: reduceMotion ? 0 : -4 }}
								initial={{ height: 0, opacity: 0, y: reduceMotion ? 0 : -4 }}
							>
								Uncommitted files in this worktree will be permanently deleted.
							</motion.p>
						) : null}
					</AnimatePresence>
				</div>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isBusy}>Keep worktree</AlertDialogCancel>
					<AlertDialogAction
						disabled={isBusy}
						onClick={() => onConfirm(force)}
						variant="destructive"
					>
						{isBusy ? <LoaderCircle className="animate-spin" /> : <Trash2 />}
						{isBusy
							? "Removing"
							: force
								? "Force remove worktree"
								: "Remove worktree"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
