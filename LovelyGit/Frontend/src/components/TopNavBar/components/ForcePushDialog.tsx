import { ShieldAlert } from "lucide-react";
import { motion, useReducedMotion } from "motion/react";
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

export function ForcePushDialog({
	branchName,
	isBusy,
	onConfirm,
	onOpenChange,
	open,
}: {
	branchName: string | null;
	isBusy: boolean;
	onConfirm: () => void;
	onOpenChange: (open: boolean) => void;
	open: boolean;
}) {
	const reduceMotion = useReducedMotion();
	const branch = branchName || "the current branch";
	return (
		<AlertDialog onOpenChange={onOpenChange} open={open}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogMedia className="bg-destructive/10 text-destructive">
						<motion.span
							animate={{ rotate: 0, scale: 1 }}
							initial={{
								rotate: reduceMotion ? 0 : -8,
								scale: reduceMotion ? 1 : 0.86,
							}}
							transition={{ type: "spring", stiffness: 360, damping: 22 }}
						>
							<ShieldAlert aria-hidden="true" />
						</motion.span>
					</AlertDialogMedia>
					<AlertDialogTitle>Force push {branch}?</AlertDialogTitle>
					<AlertDialogDescription>
						This rewrites remote history. LovelyGit will use a force-with-lease,
						so Git refuses the push if the remote changed since your last fetch.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<div className="rounded-lg border bg-card p-3 text-xs text-muted-foreground">
					Fetch first if somebody else may have pushed. Commits added after that
					fetch remain protected by the lease.
				</div>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isBusy}>Cancel</AlertDialogCancel>
					<AlertDialogAction
						disabled={isBusy}
						onClick={onConfirm}
						variant="destructive"
					>
						{isBusy ? "Force pushing…" : "Force push with lease"}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
