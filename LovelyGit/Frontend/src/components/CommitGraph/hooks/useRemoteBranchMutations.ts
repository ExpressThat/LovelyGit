import { useState } from "react";
import { NativeMessageType } from "@/lib/nativeMessaging";
import type { BranchMutate } from "./useBranchMutationRunner";

export function useRemoteBranchMutations({
	mutate,
	onCurrentBranchNameChange,
	onRepositoryChanged,
	repositoryId,
}: {
	mutate: BranchMutate;
	onCurrentBranchNameChange: (branchName: string) => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
}) {
	const [checkoutRemoteBranchName, setCheckoutRemoteBranchName] = useState<
		string | null
	>(null);
	const [deleteRemoteBranchName, setDeleteRemoteBranchName] = useState<
		string | null
	>(null);

	const checkoutRemoteBranch = (localBranchName: string) => {
		if (!checkoutRemoteBranchName) return;
		const remoteBranchName = checkoutRemoteBranchName;
		return mutate(
			remoteBranchName,
			`Creating ${localBranchName} from ${remoteBranchName}`,
			`Switched to ${localBranchName}, tracking ${remoteBranchName}`,
			{
				arguments: {
					localBranchName,
					remoteBranchName,
					repositoryId: repositoryId ?? "",
				},
				commandType: NativeMessageType.CheckoutRemoteBranch,
			},
			() => {
				setCheckoutRemoteBranchName(null);
				onCurrentBranchNameChange(localBranchName);
				onRepositoryChanged();
			},
		);
	};

	const deleteRemoteBranch = () => {
		if (!deleteRemoteBranchName) return;
		const remoteBranchName = deleteRemoteBranchName;
		return mutate(
			remoteBranchName,
			`Deleting ${remoteBranchName}`,
			`Deleted ${remoteBranchName} from its remote`,
			{
				arguments: {
					remoteBranchName,
					repositoryId: repositoryId ?? "",
				},
				commandType: NativeMessageType.DeleteRemoteBranch,
			},
			() => {
				setDeleteRemoteBranchName(null);
				onRepositoryChanged();
			},
		);
	};

	return {
		checkoutRemoteBranch,
		checkoutRemoteBranchName,
		deleteRemoteBranch,
		deleteRemoteBranchName,
		setCheckoutRemoteBranchName,
		setDeleteRemoteBranchName,
	};
}
