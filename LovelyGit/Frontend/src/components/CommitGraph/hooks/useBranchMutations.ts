import { useState } from "react";
import { NativeMessageType } from "@/lib/nativeMessaging";
import type { BranchAction } from "../components/BranchContextMenu";
import { useBranchMutationRunner } from "./useBranchMutationRunner";
import { useRemoteBranchMutations } from "./useRemoteBranchMutations";

export function useBranchMutations({
	currentBranchName,
	onCurrentBranchNameChange,
	onRepositoryChanged,
	onUpstreamChanged,
	remoteName,
	repositoryId,
}: {
	currentBranchName: string | null;
	onCurrentBranchNameChange: (branchName: string) => void;
	onRepositoryChanged: () => void;
	onUpstreamChanged: (branchName: string, upstreamName: string | null) => void;
	remoteName: string | null;
	repositoryId: string | null;
}) {
	const { busyBranch, mutate } = useBranchMutationRunner(repositoryId);
	const [comparisonBranchName, setComparisonBranchName] = useState<
		string | null
	>(null);
	const [deleteBranchName, setDeleteBranchName] = useState<string | null>(null);
	const [renameBranchName, setRenameBranchName] = useState<string | null>(null);
	const [upstreamBranchName, setUpstreamBranchName] = useState<string | null>(
		null,
	);

	const remoteBranches = useRemoteBranchMutations({
		mutate,
		onCurrentBranchNameChange,
		onRepositoryChanged,
		repositoryId,
	});

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
			onRepositoryChanged,
		);
	};

	const manageBranch = (action: BranchAction, branchName: string) => {
		if (action === "compare") setComparisonBranchName(branchName);
		else if (action === "checkout") void checkoutBranch(branchName);
		else if (action === "checkoutRemote")
			remoteBranches.setCheckoutRemoteBranchName(branchName);
		else if (action === "deleteRemote")
			remoteBranches.setDeleteRemoteBranchName(branchName);
		else if (action === "push") void pushBranch(branchName);
		else if (action === "rename") setRenameBranchName(branchName);
		else if (action === "upstream") setUpstreamBranchName(branchName);
		else setDeleteBranchName(branchName);
	};
	const manageUpstream = (upstreamName: string | null) => {
		if (!upstreamBranchName) return;
		const branchName = upstreamBranchName;
		return mutate(
			branchName,
			upstreamName
				? `Setting ${branchName} upstream`
				: `Unsetting ${branchName} upstream`,
			upstreamName
				? `${branchName} now tracks ${upstreamName}`
				: `Removed ${branchName} upstream`,
			{
				arguments: {
					branchName,
					repositoryId: repositoryId ?? "",
					upstreamName,
				},
				commandType: NativeMessageType.ManageBranchUpstream,
			},
			() => {
				onUpstreamChanged(branchName, upstreamName);
				setUpstreamBranchName(null);
				onRepositoryChanged();
			},
		);
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
		comparisonBranchName,
		currentBranchName,
		deleteBranch,
		deleteBranchName,
		manageBranch,
		manageUpstream,
		mutate,
		remoteName,
		renameBranchName,
		renameBranch,
		repositoryId,
		setDeleteBranchName,
		setComparisonBranchName,
		setRenameBranchName,
		setUpstreamBranchName,
		upstreamBranchName,
		...remoteBranches,
		onCurrentBranchNameChange,
		onRepositoryChanged,
	};
}

export type BranchMutationController = ReturnType<typeof useBranchMutations>;
