import type { CommitGraphRow } from "@/generated/types";

export type CommitGraphViewProps = {
	onCurrentBranchNameChange?: (branchName: string | null) => void;
	onOpenWorkingChanges: () => void;
	onRepositoryChanged: () => void;
	onSelectCommit: (row: CommitGraphRow) => void;
	refreshToken?: number;
	repositoryId: string | null;
	selectedCommitHash: string | null;
};
