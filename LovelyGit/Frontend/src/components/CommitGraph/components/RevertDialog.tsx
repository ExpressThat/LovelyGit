import type { CommitGraphRow } from "@/generated/types";
import { CommitOperationDialog } from "./CommitOperationDialog";

export function RevertDialog({
	commit,
	currentBranchName,
	onOpenChange,
	onOpenWorkingChanges,
	onRepositoryChanged,
	repositoryId,
}: {
	commit: CommitGraphRow | null;
	currentBranchName: string | null;
	onOpenChange: (commit: CommitGraphRow | null) => void;
	onOpenWorkingChanges: () => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
}) {
	return (
		<CommitOperationDialog
			commit={commit}
			currentBranchName={currentBranchName}
			mode="revert"
			onOpenChange={onOpenChange}
			onOpenWorkingChanges={onOpenWorkingChanges}
			onRepositoryChanged={onRepositoryChanged}
			repositoryId={repositoryId}
		/>
	);
}
