import { ListRestart, LoaderCircle } from "lucide-react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useState } from "react";
import { toast } from "sonner";
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
	type GitReflogEntry,
	GitResetMode,
	type GitResetMode as GitResetModeValue,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { shortHash } from "../utils/format";
import { ResetModeOption, resetOptions } from "./ResetModeOption";

export function ReflogResetDialog({
	currentBranchName,
	entry,
	onClose,
	onOpenWorkingChanges,
	onRepositoryChanged,
	repositoryId,
}: {
	currentBranchName: string;
	entry: GitReflogEntry;
	onClose: () => void;
	onOpenWorkingChanges: () => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
}) {
	const [confirmation, setConfirmation] = useState("");
	const [isRunning, setIsRunning] = useState(false);
	const [mode, setMode] = useState<GitResetModeValue>(GitResetMode.Mixed);
	const reduceMotion = useReducedMotion();
	const isHard = mode === GitResetMode.Hard;
	const canSubmit =
		Boolean(repositoryId) &&
		!isRunning &&
		(!isHard || confirmation === currentBranchName);
	const reset = async () => {
		if (!repositoryId || !canSubmit) return;
		setIsRunning(true);
		const toastId = toast.loading(
			`Resetting ${currentBranchName} to ${entry.selector}`,
		);
		try {
			await sendRequestWithResponse(
				{
					arguments: {
						commitHash: entry.newHash,
						repositoryId,
						resetMode: mode,
					},
					commandType: "ResetCurrentBranchToCommit",
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			onClose();
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
		<Dialog open onOpenChange={(open) => !open && !isRunning && onClose()}>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						void reset();
					}}
				>
					<DialogHeader>
						<DialogTitle>
							Reset {currentBranchName} to {entry.selector}?
						</DialogTitle>
						<DialogDescription>
							Move the current branch to {shortHash(entry.newHash)} from its
							reflog history.
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
									className="grid gap-2 overflow-hidden rounded-lg border border-destructive/35 bg-destructive/8 p-3"
									exit={{ height: 0, opacity: 0, y: reduceMotion ? 0 : -4 }}
									initial={{ height: 0, opacity: 0, y: reduceMotion ? 0 : -4 }}
								>
									<p className="text-destructive text-xs">
										Hard reset permanently discards tracked staged and
										working-tree changes.
									</p>
									<Input
										aria-label={`Type ${currentBranchName} to confirm hard reset`}
										onChange={(event) =>
											setConfirmation(event.currentTarget.value)
										}
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
					<DialogFooter>
						<Button
							disabled={!canSubmit}
							type="submit"
							variant={isHard ? "destructive" : "default"}
						>
							{isRunning ? (
								<LoaderCircle className="animate-spin" />
							) : (
								<ListRestart />
							)}
							{isRunning ? "Resetting" : `${mode} reset`}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
