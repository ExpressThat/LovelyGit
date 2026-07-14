import { useState } from "react";
import { toast } from "sonner";
import { ListRestart, LoaderCircle } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import {
	type CommitGraphRow,
	GitResetMode,
	type GitResetMode as GitResetModeValue,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { AnimatePresence, motion, useReducedMotion } from "@/lib/motion";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { shortHash } from "../utils/format";
import { ResetModeOption, resetOptions } from "./ResetModeOption";

export function ResetCommitDialog(props: {
	commit: CommitGraphRow | null;
	currentBranchName: string | null;
	onOpenChange: (commit: CommitGraphRow | null) => void;
	onOpenWorkingChanges: () => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
}) {
	if (!props.commit || !props.currentBranchName) return null;
	return (
		<ResetCommitDialogContent
			key={`${props.commit.commit.hash}:${props.currentBranchName}`}
			{...props}
			commit={props.commit}
			currentBranchName={props.currentBranchName}
		/>
	);
}

function ResetCommitDialogContent({
	commit,
	currentBranchName,
	onOpenChange,
	onOpenWorkingChanges,
	onRepositoryChanged,
	repositoryId,
}: {
	commit: CommitGraphRow;
	currentBranchName: string;
	onOpenChange: (commit: CommitGraphRow | null) => void;
	onOpenWorkingChanges: () => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
}) {
	const [confirmation, setConfirmation] = useState("");
	const [isRunning, setIsRunning] = useState(false);
	const [mode, setMode] = useState<GitResetModeValue>(GitResetMode.Mixed);
	const reduceMotion = useReducedMotion();
	const hash = shortHash(commit.commit.hash);
	const isHard = mode === GitResetMode.Hard;
	const canSubmit =
		Boolean(repositoryId) &&
		!isRunning &&
		(!isHard || confirmation === currentBranchName);

	const resetBranch = async () => {
		if (!repositoryId || !canSubmit) return;
		setIsRunning(true);
		const toastId = toast.loading(`Resetting ${currentBranchName} to ${hash}`);
		try {
			await sendRequestWithResponse(
				{
					arguments: {
						commitHash: commit.commit.hash,
						repositoryId,
						resetMode: mode,
					},
					commandType: NativeMessageType.ResetCurrentBranchToCommit,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			onOpenChange(null);
			onRepositoryChanged();
			toast.success(`${mode} reset completed on ${currentBranchName}`, {
				id: toastId,
			});
			if (!isHard) onOpenWorkingChanges();
		} catch (error) {
			toast.error(error instanceof Error ? error.message : "Reset failed.", {
				id: toastId,
			});
		} finally {
			setIsRunning(false);
		}
	};

	return (
		<Dialog
			onOpenChange={(open) => !open && !isRunning && onOpenChange(null)}
			open
		>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						void resetBranch();
					}}
				>
					<DialogHeader>
						<DialogTitle>
							Reset {currentBranchName} to {hash}?
						</DialogTitle>
						<DialogDescription>
							Choose what happens to staged and working-tree changes.
						</DialogDescription>
					</DialogHeader>
					<fieldset className="grid gap-2 py-4">
						<legend className="sr-only">Reset mode</legend>
						{resetOptions.map((option) => (
							<ResetModeOption
								key={option.mode}
								mode={mode}
								onSelect={setMode}
								option={option}
								reduceMotion={Boolean(reduceMotion)}
							/>
						))}
						<AnimatePresence initial={false}>
							{isHard ? (
								<motion.div
									animate={{ height: "auto", opacity: 1, y: 0 }}
									exit={{ height: 0, opacity: 0, y: reduceMotion ? 0 : -4 }}
									initial={{ height: 0, opacity: 0, y: reduceMotion ? 0 : -4 }}
									className="grid gap-2 overflow-hidden rounded-lg border border-destructive/35 bg-destructive/8 p-3"
								>
									<p className="text-destructive text-xs">
										Hard reset permanently discards tracked staged and
										working-tree changes.
									</p>
									<Input
										aria-label={`Type ${currentBranchName} to confirm hard reset`}
										autoComplete="off"
										onChange={(event) => setConfirmation(event.target.value)}
										onInput={(event) =>
											setConfirmation(event.currentTarget.value)
										}
										placeholder={`Type ${currentBranchName} to confirm`}
										value={confirmation}
									/>
								</motion.div>
							) : null}
						</AnimatePresence>
					</fieldset>
					<DialogFooter className="mx-0 mb-0 px-0 pb-0">
						<Button
							disabled={!canSubmit}
							type="submit"
							variant={isHard ? "destructive" : "default"}
						>
							{isRunning ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : (
								<ListRestart aria-hidden="true" />
							)}
							{isRunning ? "Resetting" : `${mode} reset`}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
