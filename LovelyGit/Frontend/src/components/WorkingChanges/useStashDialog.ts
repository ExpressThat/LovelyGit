import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { type RepositoryStashItem, StashAction } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function useStashDialog(
	repositoryId: string,
	onRepositoryChanged: () => Promise<void> | void,
	controlled?: {
		onOpenChange: (open: boolean) => void;
		open: boolean;
	},
) {
	const [busyAction, setBusyAction] = useState<StashAction | null>(null);
	const [dropTarget, setDropTarget] = useState<RepositoryStashItem | null>(
		null,
	);
	const [includeUntracked, setIncludeUntracked] = useState(true);
	const [isLoading, setIsLoading] = useState(false);
	const [loadError, setLoadError] = useState<string | null>(null);
	const [message, setMessage] = useState("");
	const [internalOpen, setInternalOpen] = useState(false);
	const open = controlled?.open ?? internalOpen;
	const setOpen = controlled?.onOpenChange ?? setInternalOpen;
	const [restoreIndex, setRestoreIndex] = useState(true);
	const [stashes, setStashes] = useState<RepositoryStashItem[]>([]);

	const loadStashes = useCallback(async () => {
		setIsLoading(true);
		setLoadError(null);
		try {
			const response = await sendRequestWithResponse({
				arguments: { knownRepositoryId: repositoryId },
				commandType: NativeMessageType.GetRepositoryRefs,
			});
			setStashes(response.stashes);
		} catch (error) {
			setLoadError(
				error instanceof Error ? error.message : "Failed to load stashes.",
			);
		} finally {
			setIsLoading(false);
		}
	}, [repositoryId]);

	useEffect(() => {
		if (open) void loadStashes();
	}, [open, loadStashes]);

	const runAction = async (
		action: StashAction,
		stash?: RepositoryStashItem,
	) => {
		if (busyAction) return;
		setBusyAction(action);
		const actionLabel = stashActionLabel(action);
		const toastId = toast.loading(`${actionLabel} in progress`);
		try {
			await sendRequestWithResponse(
				{
					arguments: {
						action,
						includeUntracked,
						message:
							action === StashAction.Create ? message.trim() || null : null,
						repositoryId,
						restoreIndex,
						selector: stash?.selector ?? null,
					},
					commandType: NativeMessageType.ManageStash,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			if (action === StashAction.Create) setMessage("");
			setDropTarget(null);
			await Promise.all([loadStashes(), onRepositoryChanged()]);
			toast.success(`${actionLabel} complete`, { id: toastId });
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : `${actionLabel} failed.`,
				{ id: toastId },
			);
		} finally {
			setBusyAction(null);
		}
	};

	return {
		busyAction,
		dropTarget,
		includeUntracked,
		isLoading,
		loadError,
		message,
		open,
		restoreIndex,
		runAction,
		setDropTarget,
		setIncludeUntracked,
		setMessage,
		setOpen,
		setRestoreIndex,
		stashes,
	};
}

function stashActionLabel(action: StashAction) {
	return action === StashAction.Create
		? "Stash changes"
		: action === StashAction.Apply
			? "Apply stash"
			: action === StashAction.Pop
				? "Pop stash"
				: "Delete stash";
}
