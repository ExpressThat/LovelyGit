import { useState } from "react";
import type { GitReflogEntry } from "@/generated/types";

export function useReflogManagement() {
	const [branchName, setBranchName] = useState<string | null>(null);
	const [resetTarget, setResetTarget] = useState<GitReflogEntry | null>(null);
	return {
		branchName,
		close: () => setBranchName(null),
		closeReset: () => setResetTarget(null),
		open: setBranchName,
		resetTarget,
		startReset: (entry: GitReflogEntry) => {
			setBranchName(null);
			setResetTarget(entry);
		},
	};
}

export type ReflogManagementController = ReturnType<typeof useReflogManagement>;
