import type { CommitGraphRow } from "@/generated/types";
import { CommitOperationDialog } from "./CommitOperationDialog";

export function RevertDialog({
	commits,
	currentBranchName,
	onOpenChange,
	onOpenWorkingChanges,
	onRepositoryChanged,
	repositoryId,
}: {
	commits: CommitGraphRow[] | null;
	currentBranchName: string | null;
	onOpenChange: (commits: CommitGraphRow[] | null) => void;
	onOpenWorkingChanges: () => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
}) {
	return (
		<CommitOperationDialog
			commits={commits}
			currentBranchName={currentBranchName}
			mode="revert"
			onOpenChange={onOpenChange}
			onOpenWorkingChanges={onOpenWorkingChanges}
			onRepositoryChanged={onRepositoryChanged}
			repositoryId={repositoryId}
		/>
	);
}
