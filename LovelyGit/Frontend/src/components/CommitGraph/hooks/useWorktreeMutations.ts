import { useState } from "react";
import { toast } from "sonner";
import type {
	KnownGitRepository,
	RepositoryWorktreeItem,
	WorktreeMutationAction,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { nativeDialogTimeoutMs } from "@/lib/nativeDialogTimeout";
import { useRepositoryContext } from "@/lib/repositoryContext";

export function useWorktreeMutations({
	onRepositoryChanged,
	repositoryId,
}: {
	onRepositoryChanged: () => void;
	repositoryId: string | null;
}) {
	const {
		reconcileRepository,
		reconcileRepositoryRemoval,
		setCurrentRepositoryId,
	} = useRepositoryContext();
	const [busyPath, setBusyPath] = useState<string | null>(null);
	const [createBranchName, setCreateBranchName] = useState<string | null>(null);
	const [lockTarget, setLockTarget] = useState<RepositoryWorktreeItem | null>(
		null,
	);
	const [removeTarget, setRemoveTarget] =
		useState<RepositoryWorktreeItem | null>(null);

	const chooseDestination = () =>
		sendRequestWithResponse(
			{ commandType: "ChooseWorktreeDestination" },
			{ timeoutMs: nativeDialogTimeoutMs },
		);

	const create = async (worktreePath: string) => {
		if (!repositoryId || !createBranchName || busyPath) return;
		const branchName = createBranchName;
		setBusyPath(worktreePath);
		const toastId = toast.loading(`Creating worktree for ${branchName}`);
		try {
			await sendRequestWithResponse(
				{
					arguments: { branchName, repositoryId, worktreePath },
					commandType: "CreateWorktree",
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			setCreateBranchName(null);
			onRepositoryChanged();
			toast.success(`Created worktree for ${branchName}`, { id: toastId });
		} catch (error) {
			toast.error(message(error, "Failed to create the worktree."), {
				id: toastId,
			});
		} finally {
			setBusyPath(null);
		}
	};

	const mutate = async (
		action: WorktreeMutationAction,
		worktree: RepositoryWorktreeItem,
		options: { force?: boolean; lockReason?: string } = {},
	) => {
		if (!repositoryId || busyPath) return;
		setBusyPath(worktree.path);
		const toastId = toast.loading(loadingMessage(action, worktree));
		try {
			const opened = await sendRequestWithResponse(
				{
					arguments: {
						action,
						force: options.force ?? false,
						lockReason: options.lockReason?.trim() || null,
						repositoryId,
						worktreePath: worktree.path,
					},
					commandType: "ManageWorktree",
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			await afterSuccess(
				action,
				opened,
				reconcileRepository,
				reconcileRepositoryRemoval,
				setCurrentRepositoryId,
			);
			if (action === "Lock") setLockTarget(null);
			if (action === "Remove") setRemoveTarget(null);
			if (action === "Lock" || action === "Unlock" || action === "Remove") {
				onRepositoryChanged();
			}
			toast.success(successMessage(action, worktree), { id: toastId });
		} catch (error) {
			toast.error(message(error, "Worktree operation failed."), {
				id: toastId,
			});
		} finally {
			setBusyPath(null);
		}
	};

	const manage = (
		action: WorktreeMutationAction,
		worktree: RepositoryWorktreeItem,
	) => {
		if (action === "Lock") setLockTarget(worktree);
		else if (action === "Remove") setRemoveTarget(worktree);
		else void mutate(action, worktree);
	};

	return {
		busyPath,
		chooseDestination,
		create,
		createBranchName,
		lockTarget,
		manage,
		mutate,
		removeTarget,
		setCreateBranchName,
		setLockTarget,
		setRemoveTarget,
	};
}

async function afterSuccess(
	action: WorktreeMutationAction,
	opened: KnownGitRepository | null,
	reconcile: (repository: KnownGitRepository) => void,
	remove: (repositoryId: string) => void,
	select: (repositoryId: string | null) => Promise<void>,
) {
	if (action === "Remove" && opened) remove(opened.id);
	if (action === "Open" && opened) {
		reconcile(opened);
		await select(opened.id);
	}
}

function loadingMessage(
	action: WorktreeMutationAction,
	worktree: RepositoryWorktreeItem,
) {
	const verbs: Record<WorktreeMutationAction, string> = {
		Lock: "Locking",
		Open: "Opening",
		Remove: "Removing",
		Reveal: "Revealing",
		Terminal: "Opening terminal for",
		Unlock: "Unlocking",
	};
	return `${verbs[action]} ${worktree.branchName ?? "worktree"}`;
}

function successMessage(
	action: WorktreeMutationAction,
	worktree: RepositoryWorktreeItem,
) {
	const label = worktree.branchName ?? "detached worktree";
	const verbs: Record<WorktreeMutationAction, string> = {
		Lock: "Locked",
		Open: "Opened",
		Remove: "Removed",
		Reveal: "Revealed",
		Terminal: "Opened terminal for",
		Unlock: "Unlocked",
	};
	return `${verbs[action]} ${label}`;
}

function message(error: unknown, fallback: string) {
	return error instanceof Error ? error.message : fallback;
}

export type WorktreeMutationController = ReturnType<
	typeof useWorktreeMutations
>;
