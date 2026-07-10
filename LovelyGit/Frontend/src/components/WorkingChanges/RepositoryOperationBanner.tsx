import {
	GitCommitHorizontal,
	GitMerge,
	ListRestart,
	LoaderCircle,
	Play,
	X,
} from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
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
import { Button } from "@/components/ui/button";
import type { GitRepositoryOperationKind } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function RepositoryOperationBanner({
	conflictCount,
	onRefresh,
	onRepositoryChanged,
	repositoryId,
	workingTreeCount,
}: {
	conflictCount: number;
	onRefresh: () => Promise<void> | void;
	onRepositoryChanged: () => Promise<void> | void;
	repositoryId: string;
	workingTreeCount: number;
}) {
	const [abortOpen, setAbortOpen] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [isBusy, setIsBusy] = useState(false);
	const [operation, setOperation] = useState<GitRepositoryOperationKind | null>(
		null,
	);
	const loadOperation = useCallback(async () => {
		try {
			const response = await sendRequestWithResponse({
				arguments: { repositoryId },
				commandType: NativeMessageType.GetRepositoryOperationState,
			});
			setOperation(response.operation);
			setError(null);
		} catch (loadError) {
			setError(
				loadError instanceof Error
					? loadError.message
					: "Could not read the repository operation state.",
			);
		}
	}, [repositoryId]);

	useEffect(() => {
		if (workingTreeCount >= 0) {
			void loadOperation();
		}
	}, [loadOperation, workingTreeCount]);

	if (!operation && !error) {
		return null;
	}

	const label =
		operation === "CherryPick"
			? "Cherry-pick"
			: operation === "Rebase"
				? "Rebase"
				: "Merge";
	const refreshAfterMutation = async () => {
		await Promise.all([onRefresh(), onRepositoryChanged()]);
		await loadOperation();
	};
	const continueOperation = async () => {
		if (!operation || isBusy || conflictCount > 0) {
			return;
		}

		setIsBusy(true);
		setError(null);
		const toastId = toast.loading(`Continuing ${label.toLowerCase()}`);
		try {
			const response = await sendRequestWithResponse(
				{
					arguments: { operation, repositoryId },
					commandType: NativeMessageType.ContinueRepositoryOperation,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			if (response.isCompleted) {
				toast.success(`${label} completed`, { id: toastId });
			} else {
				toast.warning(`${label} paused again`, {
					description: response.message ?? "Resolve the remaining conflicts.",
					id: toastId,
				});
			}
			await refreshAfterMutation();
		} catch (continueError) {
			const message =
				continueError instanceof Error
					? continueError.message
					: `Could not continue the ${label.toLowerCase()}.`;
			setError(message);
			toast.error(message, { id: toastId });
		} finally {
			setIsBusy(false);
		}
	};
	const abortOperation = async () => {
		if (!operation || isBusy) {
			return;
		}

		setIsBusy(true);
		setError(null);
		const toastId = toast.loading(`Aborting ${label.toLowerCase()}`);
		try {
			await sendRequestWithResponse(
				{
					arguments: { operation, repositoryId },
					commandType: NativeMessageType.AbortRepositoryOperation,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			setAbortOpen(false);
			toast.success(`${label} aborted`, { id: toastId });
			await refreshAfterMutation();
		} catch (abortError) {
			const message =
				abortError instanceof Error
					? abortError.message
					: `Could not abort the ${label.toLowerCase()}.`;
			setError(message);
			toast.error(message, { id: toastId });
		} finally {
			setIsBusy(false);
		}
	};

	return (
		<>
			<div className="grid gap-3 rounded-lg border border-primary/30 bg-primary/5 p-3">
				<div className="flex items-start gap-2">
					{operation === "CherryPick" ? (
						<GitCommitHorizontal
							aria-hidden="true"
							className="mt-0.5 size-4 text-primary"
						/>
					) : operation === "Rebase" ? (
						<ListRestart
							aria-hidden="true"
							className="mt-0.5 size-4 text-primary"
						/>
					) : (
						<GitMerge
							aria-hidden="true"
							className="mt-0.5 size-4 text-primary"
						/>
					)}
					<div className="min-w-0 flex-1">
						<p className="font-medium text-sm">{label} in progress</p>
						<p className="mt-0.5 text-muted-foreground text-xs">
							{conflictCount > 0
								? `Resolve and stage ${conflictCount} conflicted ${conflictCount === 1 ? "file" : "files"} before continuing.`
								: "Continue to apply the staged resolution, or abort to restore the branch."}
						</p>
					</div>
				</div>
				{error ? <p className="text-destructive text-xs">{error}</p> : null}
				<div className="flex justify-end gap-2">
					<Button
						disabled={isBusy}
						onClick={() => setAbortOpen(true)}
						size="sm"
						type="button"
						variant="outline"
					>
						<X aria-hidden="true" />
						Abort
					</Button>
					<Button
						disabled={isBusy || conflictCount > 0}
						onClick={() => void continueOperation()}
						size="sm"
						title={
							conflictCount > 0
								? "Resolve and stage all conflicts before continuing"
								: `Continue ${label.toLowerCase()}`
						}
						type="button"
					>
						{isBusy ? (
							<LoaderCircle aria-hidden="true" className="animate-spin" />
						) : (
							<Play aria-hidden="true" />
						)}
						Continue
					</Button>
				</div>
			</div>
			<AlertDialog open={abortOpen} onOpenChange={setAbortOpen}>
				<AlertDialogContent>
					<AlertDialogHeader>
						<AlertDialogMedia className="bg-destructive/10 text-destructive">
							<X aria-hidden="true" />
						</AlertDialogMedia>
						<AlertDialogTitle>
							Abort the {label.toLowerCase()}?
						</AlertDialogTitle>
						<AlertDialogDescription>
							Git will restore the branch and working tree to their state before
							the {label.toLowerCase()} started.
						</AlertDialogDescription>
					</AlertDialogHeader>
					<AlertDialogFooter>
						<AlertDialogCancel disabled={isBusy}>
							Keep resolving
						</AlertDialogCancel>
						<AlertDialogAction
							disabled={isBusy}
							onClick={() => void abortOperation()}
							variant="destructive"
						>
							{isBusy ? "Aborting" : `Abort ${label.toLowerCase()}`}
						</AlertDialogAction>
					</AlertDialogFooter>
				</AlertDialogContent>
			</AlertDialog>
		</>
	);
}
