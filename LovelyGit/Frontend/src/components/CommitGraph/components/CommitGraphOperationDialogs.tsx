import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import { BranchIntegrationDialog } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { CommitGraphRow, RepositoryRefsResponse } from "@/generated/types";
import { CherryPickDialog } from "./CherryPickDialog";
import { ResetCommitDialog } from "./ResetCommitDialog";
import { RevertDialog } from "./RevertDialog";

type IntegrationTarget = {
	branchName: string;
	mode: BranchIntegrationMode;
} | null;

export function CommitGraphOperationDialogs({
	cherryPickCommit,
	currentBranchName,
	integrationTarget,
	onOpenWorkingChanges,
	onRepositoryChanged,
	repositoryId,
	repositoryRefs,
	resetCommit,
	revertCommit,
	setCherryPickCommit,
	setIntegrationTarget,
	setResetCommit,
	setRevertCommit,
}: {
	cherryPickCommit: CommitGraphRow | null;
	currentBranchName: string | null;
	integrationTarget: IntegrationTarget;
	onOpenWorkingChanges: () => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
	repositoryRefs: RepositoryRefsResponse | null;
	resetCommit: CommitGraphRow | null;
	revertCommit: CommitGraphRow | null;
	setCherryPickCommit: (commit: CommitGraphRow | null) => void;
	setIntegrationTarget: (target: IntegrationTarget) => void;
	setResetCommit: (commit: CommitGraphRow | null) => void;
	setRevertCommit: (commit: CommitGraphRow | null) => void;
}) {
	return (
		<>
			<BranchIntegrationDialog
				branches={(repositoryRefs?.refs ?? []).filter(
					(ref) => ref.kind === "Local",
				)}
				currentBranchName={currentBranchName}
				mode={integrationTarget?.mode ?? null}
				onOpenChange={(mode) => {
					if (mode === null) setIntegrationTarget(null);
				}}
				onOpenWorkingChanges={onOpenWorkingChanges}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId={repositoryId}
				targetBranchName={integrationTarget?.branchName}
			/>
			<CherryPickDialog
				commit={cherryPickCommit}
				currentBranchName={currentBranchName}
				onOpenChange={setCherryPickCommit}
				onOpenWorkingChanges={onOpenWorkingChanges}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId={repositoryId}
			/>
			<RevertDialog
				commit={revertCommit}
				currentBranchName={currentBranchName}
				onOpenChange={setRevertCommit}
				onOpenWorkingChanges={onOpenWorkingChanges}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId={repositoryId}
			/>
			<ResetCommitDialog
				commit={resetCommit}
				currentBranchName={currentBranchName}
				onOpenChange={setResetCommit}
				onOpenWorkingChanges={onOpenWorkingChanges}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId={repositoryId}
			/>
		</>
	);
}
