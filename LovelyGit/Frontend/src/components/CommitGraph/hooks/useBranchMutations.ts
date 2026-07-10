import { useState } from "react";
import { toast } from "sonner";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";
import type { BranchAction } from "../components/BranchContextMenu";

export function useBranchMutations({
	currentBranchName,
	onCurrentBranchNameChange,
	onRepositoryChanged,
	remoteName,
	repositoryId,
}: {
	currentBranchName: string | null;
	onCurrentBranchNameChange: (branchName: string) => void;
	onRepositoryChanged: () => void;
	remoteName: string | null;
	repositoryId: string | null;
}) {
	const [busyBranch, setBusyBranch] = useState<string | null>(null);
	const [deleteBranchName, setDeleteBranchName] = useState<string | null>(null);
	const [renameBranchName, setRenameBranchName] = useState<string | null>(null);

	const mutate = async (
		branchName: string,
		loadingMessage: string,
		successMessage: string,
		request: Parameters<typeof sendRequestWithResponse>[0],
		afterSuccess?: () => void,
	) => {
		if (!repositoryId || busyBranch) return;
		setBusyBranch(branchName);
		const toastId = toast.loading(loadingMessage);
		try {
			await sendRequestWithResponse(request, {
				timeoutMs: gitMutationTimeoutMs,
			});
			afterSuccess?.();
			toast.success(successMessage, { id: toastId });
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Git operation failed.",
				{
					id: toastId,
				},
			);
		} finally {
			setBusyBranch(null);
		}
	};

	const checkoutBranch = (branchName: string) =>
		mutate(
			branchName,
			`Switching to ${branchName}`,
			`Switched to ${branchName}`,
			{
				arguments: { branchName, repositoryId: repositoryId ?? "" },
				commandType: NativeMessageType.CheckoutBranch,
			},
			() => {
				onCurrentBranchNameChange(branchName);
				onRepositoryChanged();
			},
		);

	const pushBranch = (branchName: string) => {
		if (!remoteName) return;
		return mutate(
			branchName,
			`Pushing ${branchName} to ${remoteName}`,
			`Pushed ${branchName} to ${remoteName}`,
			{
				arguments: {
					branchName,
					remoteName,
					repositoryId: repositoryId ?? "",
				},
				commandType: NativeMessageType.PushBranch,
			},
		);
	};

	const manageBranch = (action: BranchAction, branchName: string) => {
		if (action === "checkout") void checkoutBranch(branchName);
		else if (action === "push") void pushBranch(branchName);
		else if (action === "rename") setRenameBranchName(branchName);
		else setDeleteBranchName(branchName);
	};
	const renameBranch = (newBranchName: string) => {
		if (!renameBranchName) return;
		const oldBranchName = renameBranchName;
		return mutate(
			oldBranchName,
			`Renaming ${oldBranchName}`,
			`Renamed ${oldBranchName} to ${newBranchName}`,
			{
				arguments: {
					branchName: oldBranchName,
					newBranchName,
					repositoryId: repositoryId ?? "",
				},
				commandType: NativeMessageType.RenameBranch,
			},
			() => {
				setRenameBranchName(null);
				if (oldBranchName === currentBranchName) {
					onCurrentBranchNameChange(newBranchName);
				}
				onRepositoryChanged();
			},
		);
	};
	const deleteBranch = (force: boolean) => {
		if (!deleteBranchName) return;
		const branchName = deleteBranchName;
		return mutate(
			branchName,
			`Deleting ${branchName}`,
			`Deleted local branch ${branchName}`,
			{
				arguments: { branchName, force, repositoryId: repositoryId ?? "" },
				commandType: NativeMessageType.DeleteBranch,
			},
			() => {
				setDeleteBranchName(null);
				onRepositoryChanged();
			},
		);
	};

	return {
		busyBranch,
		currentBranchName,
		deleteBranch,
		deleteBranchName,
		manageBranch,
		mutate,
		remoteName,
		renameBranchName,
		renameBranch,
		repositoryId,
		setDeleteBranchName,
		setRenameBranchName,
		onCurrentBranchNameChange,
		onRepositoryChanged,
	};
}

export type BranchMutationController = ReturnType<typeof useBranchMutations>;
