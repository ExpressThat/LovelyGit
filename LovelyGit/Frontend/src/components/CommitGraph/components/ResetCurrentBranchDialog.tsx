import { Undo2 } from "lucide-react";
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
import {
	GitResetMode,
	type GitResetMode as GitResetModeValue,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { shortHash } from "../utils/format";

const resetDetails: Record<
	GitResetModeValue,
	{ action: string; description: string; isDestructive: boolean }
> = {
	[GitResetMode.Soft]: {
		action: "Reset --soft",
		description:
			"Move the current branch and keep index and working tree changes.",
		isDestructive: false,
	},
	[GitResetMode.Mixed]: {
		action: "Reset --mixed",
		description:
			"Move the current branch, reset the index, and keep working tree files.",
		isDestructive: false,
	},
	[GitResetMode.Hard]: {
		action: "Reset --hard",
		description:
			"Move the current branch and discard index and working tree changes.",
		isDestructive: true,
	},
};

export function ResetCurrentBranchDialog({
	commitHash,
	mode,
	onOpenChange,
	onSuccess,
	repositoryId,
}: {
	commitHash: string;
	mode: GitResetModeValue | null;
	onOpenChange: (isOpen: boolean) => void;
	onSuccess: () => void;
	repositoryId: string | null;
}) {
	const [isResetting, setIsResetting] = useState(false);
	const details = mode === null ? null : resetDetails[mode];
	const canReset = !isResetting && repositoryId !== null && mode !== null;

	const resetBranch = async () => {
		if (!canReset || repositoryId === null || mode === null) {
			return;
		}

		setIsResetting(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					commitHash,
					repositoryId,
					resetMode: mode,
				},
				commandType: NativeMessageType.ResetCurrentBranchToCommit,
			});
			toast.success(`Reset branch to ${shortHash(commitHash)}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not reset branch",
			);
		} finally {
			setIsResetting(false);
		}
	};

	return (
		<AlertDialog onOpenChange={onOpenChange} open={mode !== null}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogTitle className="flex items-center gap-2">
						<Undo2 aria-hidden="true" />
						Reset current branch
					</AlertDialogTitle>
					<AlertDialogDescription>
						{details?.description} Target commit: {shortHash(commitHash)}.
						{details?.isDestructive ? " This discards local file changes." : ""}
					</AlertDialogDescription>
				</AlertDialogHeader>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isResetting}>Cancel</AlertDialogCancel>
					<AlertDialogAction
						disabled={!canReset}
						onClick={resetBranch}
						variant={details?.isDestructive ? "destructive" : "default"}
					>
						{isResetting ? "Resetting" : details?.action}
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
