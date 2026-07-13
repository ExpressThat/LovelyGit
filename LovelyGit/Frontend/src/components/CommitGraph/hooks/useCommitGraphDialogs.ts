import { useEffect, useState } from "react";
import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { CommitGraphRow } from "@/generated/types";

export function useCommitGraphDialogs() {
	const [comparisonBase, setComparisonBase] = useState<CommitGraphRow | null>(
		null,
	);
	const [comparisonTarget, setComparisonTarget] =
		useState<CommitGraphRow | null>(null);
	const [integrationTarget, setIntegrationTarget] = useState<{
		branchName: string;
		mode: BranchIntegrationMode;
	} | null>(null);
	const [cherryPickCommits, setCherryPickCommits] = useState<
		CommitGraphRow[] | null
	>(null);
	const [checkoutCommit, setCheckoutCommit] = useState<CommitGraphRow | null>(
		null,
	);
	const [bisectCommit, setBisectCommit] = useState<CommitGraphRow | null>(null);
	const [revertCommits, setRevertCommits] = useState<CommitGraphRow[] | null>(
		null,
	);
	const [resetCommit, setResetCommit] = useState<CommitGraphRow | null>(null);
	const [tagCommit, setTagCommit] = useState<CommitGraphRow | null>(null);
	const [interactiveRebaseBase, setInteractiveRebaseBase] =
		useState<CommitGraphRow | null>(null);
	const integrateBranch = (mode: BranchIntegrationMode, branchName: string) =>
		setIntegrationTarget({ branchName, mode });

	return {
		bisectCommit,
		cherryPickCommits,
		checkoutCommit,
		comparison: {
			base: comparisonBase,
			compare: setComparisonTarget,
			setBase: setComparisonBase,
			setTarget: setComparisonTarget,
			target: comparisonTarget,
		},
		integrationTarget,
		interactiveRebaseBase,
		integrateBranch,
		resetCommit,
		revertCommits,
		setCherryPickCommit: (commit: CommitGraphRow) =>
			setCherryPickCommits([commit]),
		setCherryPickCommits,
		setBisectCommit,
		setCheckoutCommit,
		setIntegrationTarget,
		setInteractiveRebaseBase,
		setResetCommit,
		setRevertCommit: (commit: CommitGraphRow) => setRevertCommits([commit]),
		setRevertCommits,
		setTagCommit,
		tagCommit,
	};
}

export type CommitComparisonController = {
	base: CommitGraphRow | null;
	compare: (row: CommitGraphRow) => void;
	setBase: (row: CommitGraphRow | null) => void;
	setTarget: (row: CommitGraphRow | null) => void;
	target: CommitGraphRow | null;
};

export function useNotifyCurrentBranch(
	currentBranchName: string | null,
	onChange?: (branchName: string | null) => void,
) {
	useEffect(() => onChange?.(currentBranchName), [currentBranchName, onChange]);
}
