import { useState } from "react";
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
	const integrateBranch = (mode: BranchIntegrationMode, branchName: string) =>
		setIntegrationTarget({ branchName, mode });

	return {
		cherryPickCommit,
		integrationTarget,
		integrateBranch,
		resetCommit,
		revertCommit,
		setCherryPickCommit,
		setIntegrationTarget,
		setResetCommit,
		setRevertCommit,
		setTagCommit,
		tagCommit,
	};
}
