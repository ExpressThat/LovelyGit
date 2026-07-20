import {
	GitCommitHorizontal,
	LoaderCircle,
	Undo2,
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
import type { HeadCommitMessageResponse } from "@/generated/types";
import { AnimatePresence, motion, useReducedMotion } from "@/lib/motion";

export function UndoLastCommitDialog({
	error,
	isLoading,
	isOpen,
	isUndoing,
	onClose,
	onConfirm,
	preview,
}: {
	error: string | null;
	isLoading: boolean;
	isOpen: boolean;
	isUndoing: boolean;
	onClose: () => void;
	onConfirm: () => void;
	preview: HeadCommitMessageResponse | null;
}) {
	const reduceMotion = useReducedMotion();
	const canUndo = Boolean(preview?.firstParentHash);
	const contentTransition = { duration: reduceMotion ? 0 : 0.1 };
	return (
		<AlertDialog onOpenChange={(open) => !open && onClose()} open={isOpen}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogMedia className="bg-primary/10 text-primary">
						<Undo2 aria-hidden="true" />
					</AlertDialogMedia>
					<AlertDialogTitle>Undo last commit?</AlertDialogTitle>
					<AlertDialogDescription>
						Move the current branch to its first parent. The commit’s files stay
						staged, and existing working changes are preserved.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AnimatePresence initial={false} mode="wait">
					{isLoading ? (
						<motion.div
							animate={{ opacity: 1 }}
							className="flex items-center gap-2 rounded-md border bg-card p-3 text-sm text-muted-foreground"
							exit={{ opacity: 0 }}
							key="loading"
							transition={contentTransition}
						>
							<LoaderCircle className="size-4 animate-spin" /> Loading last
							commit…
						</motion.div>
					) : preview ? (
						<motion.div
							animate={{ opacity: 1, y: 0 }}
							className="grid gap-1 rounded-md border bg-card p-3"
							initial={{ opacity: 0, y: reduceMotion ? 0 : 4 }}
							key={preview.hash}
							transition={contentTransition}
						>
							<div className="flex min-w-0 items-center gap-2">
								<GitCommitHorizontal className="size-4 shrink-0 text-primary" />
								<span className="truncate font-medium text-sm">
									{preview.title}
								</span>
								<code className="ml-auto text-[10px] text-muted-foreground">
									{preview.hash.slice(0, 8)}
								</code>
							</div>
							{preview.body ? (
								<p className="line-clamp-3 whitespace-pre-wrap text-xs text-muted-foreground">
									{preview.body}
								</p>
							) : null}
							{preview.parentCount > 1 ? (
								<p className="text-amber-600 text-xs dark:text-amber-400">
									This merge commit will move to its first parent.
								</p>
							) : null}
						</motion.div>
					) : null}
				</AnimatePresence>
				{preview && !canUndo ? (
					<p className="rounded-md border border-amber-500/30 bg-amber-500/10 p-2 text-amber-700 text-xs dark:text-amber-300">
						The initial commit has no parent and cannot be undone.
					</p>
				) : null}
				{canUndo ? (
					<p className="text-xs text-muted-foreground">
						If this commit was already pushed, your local branch will diverge
						from the remote.
					</p>
				) : null}
				{error ? (
					<p className="rounded-md border border-destructive/40 bg-destructive/10 p-2 text-destructive text-xs">
						{error}
					</p>
				) : null}
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isUndoing}>Cancel</AlertDialogCancel>
					<AlertDialogAction
						disabled={!canUndo || isLoading || isUndoing}
						onClick={onConfirm}
					>
						{isUndoing ? "Undoing commit…" : "Undo commit"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
