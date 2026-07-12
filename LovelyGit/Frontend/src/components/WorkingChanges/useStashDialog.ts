import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import {
	CommitRefKind,
	type RepositoryStashItem,
	StashAction,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";
import {
	invalidateRepositoryRefs,
	loadRepositoryRefs,
} from "@/lib/repositoryRefsCache";

export function useStashDialog(
	repositoryId: string,
	onRepositoryChanged: () => Promise<void> | void,
	controlled?: {
		onOpenChange: (open: boolean) => void;
		open: boolean;
	},
) {
	const [busyAction, setBusyAction] = useState<StashAction | null>(null);
	const [branchNames, setBranchNames] = useState<string[]>([]);
	const [branchTarget, setBranchTarget] = useState<RepositoryStashItem | null>(
		null,
	);
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

	const loadStashes = useCallback(
		async (forceRefresh = false) => {
			setIsLoading(true);
			setLoadError(null);
			try {
				const response = await loadRepositoryRefs(repositoryId, forceRefresh);
				setStashes(response.stashes);
				setBranchNames(
					(response.refs ?? [])
						.filter((item) => item.kind === CommitRefKind.Local)
						.map((item) => item.name),
				);
			} catch (error) {
				setLoadError(
					error instanceof Error ? error.message : "Failed to load stashes.",
				);
			} finally {
				setIsLoading(false);
			}
		},
		[repositoryId],
	);

	useEffect(() => {
		if (open) void loadStashes();
	}, [open, loadStashes]);

	const runAction = async (
		action: StashAction,
		stash?: RepositoryStashItem,
		branchName?: string,
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
						branchName:
							action === StashAction.Branch ? (branchName ?? null) : null,
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
			if (action === StashAction.Branch) setBranchTarget(null);
			setDropTarget(null);
			await onRepositoryChanged();
			if (action === StashAction.Create) await loadStashes(true);
			else if (stash && action !== StashAction.Apply) {
				invalidateRepositoryRefs(repositoryId);
				setStashes((current) =>
					current.filter((item) => item.selector !== stash.selector),
				);
			}
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
		branchNames,
		branchTarget,
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
		setBranchTarget,
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
				: action === StashAction.Branch
					? "Create branch from stash"
					: "Delete stash";
}
