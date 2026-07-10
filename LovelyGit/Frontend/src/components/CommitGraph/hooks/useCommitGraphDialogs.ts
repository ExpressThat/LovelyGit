import { useEffect, useState } from "react";
import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { CommitGraphRow } from "@/generated/types";

export function useCommitGraphDialogs() {
	const [integrationTarget, setIntegrationTarget] = useState<{
		branchName: string;
		mode: BranchIntegrationMode;
	} | null>(null);
	const [cherryPickCommit, setCherryPickCommit] =
		useState<CommitGraphRow | null>(null);
	const [revertCommit, setRevertCommit] = useState<CommitGraphRow | null>(null);
	const [resetCommit, setResetCommit] = useState<CommitGraphRow | null>(null);
	const [tagCommit, setTagCommit] = useState<CommitGraphRow | null>(null);
	const [interactiveRebaseBase, setInteractiveRebaseBase] =
		useState<CommitGraphRow | null>(null);
	const integrateBranch = (mode: BranchIntegrationMode, branchName: string) =>
		setIntegrationTarget({ branchName, mode });

	return {
		cherryPickCommit,
		integrationTarget,
		interactiveRebaseBase,
		integrateBranch,
		resetCommit,
		revertCommit,
		setCherryPickCommit,
		setIntegrationTarget,
		setInteractiveRebaseBase,
		setResetCommit,
		setRevertCommit,
		setTagCommit,
		tagCommit,
	};
}

export function useNotifyCurrentBranch(
	currentBranchName: string | null,
	onChange?: (branchName: string | null) => void,
) {
	useEffect(() => onChange?.(currentBranchName), [currentBranchName, onChange]);
}
