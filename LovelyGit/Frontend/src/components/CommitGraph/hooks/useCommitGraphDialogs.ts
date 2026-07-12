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
	const [cherryPickCommit, setCherryPickCommit] =
		useState<CommitGraphRow | null>(null);
	const [checkoutCommit, setCheckoutCommit] = useState<CommitGraphRow | null>(
		null,
	);
	const [bisectCommit, setBisectCommit] = useState<CommitGraphRow | null>(null);
	const [revertCommit, setRevertCommit] = useState<CommitGraphRow | null>(null);
	const [resetCommit, setResetCommit] = useState<CommitGraphRow | null>(null);
	const [tagCommit, setTagCommit] = useState<CommitGraphRow | null>(null);
	const [interactiveRebaseBase, setInteractiveRebaseBase] =
		useState<CommitGraphRow | null>(null);
	const integrateBranch = (mode: BranchIntegrationMode, branchName: string) =>
		setIntegrationTarget({ branchName, mode });

	return {
		bisectCommit,
		cherryPickCommit,
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
		revertCommit,
		setCherryPickCommit,
		setBisectCommit,
		setCheckoutCommit,
		setIntegrationTarget,
		setInteractiveRebaseBase,
		setResetCommit,
		setRevertCommit,
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
