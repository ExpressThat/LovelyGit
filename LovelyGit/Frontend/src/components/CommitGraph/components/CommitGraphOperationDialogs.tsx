import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import { BranchIntegrationDialog } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import {
	type BranchCreationSource,
	CreateBranchDialog,
} from "@/components/TopNavBar/components/CreateBranchDialog";
import type {
	CommitGraphRow,
	GitReflogEntry,
	RepositoryRefsResponse,
} from "@/generated/types";
import type { ReflogManagementController } from "../hooks/useReflogManagement";
import { CherryPickDialog } from "./CherryPickDialog";
import { ReflogDialog } from "./ReflogDialog";
import { ReflogResetDialog } from "./ReflogResetDialog";
import { ResetCommitDialog } from "./ResetCommitDialog";
import { RevertDialog } from "./RevertDialog";

type IntegrationTarget = {
	branchName: string;
	mode: BranchIntegrationMode;
} | null;

export function CommitGraphOperationDialogs({
	cherryPickCommit,
	branchCreationSource,
	branchNames,
	currentBranchName,
	integrationTarget,
	onOpenWorkingChanges,
	onCreateBranchFromReflog,
	onBranchCreationClose,
	onCurrentBranchNameChange,
	onRepositoryChanged,
	repositoryId,
	repositoryRefs,
	reflogController,
	resetCommit,
	revertCommit,
	setCherryPickCommit,
	setIntegrationTarget,
	setResetCommit,
	setRevertCommit,
}: {
	cherryPickCommit: CommitGraphRow | null;
	branchCreationSource: BranchCreationSource | null;
	branchNames: string[];
	currentBranchName: string | null;
	integrationTarget: IntegrationTarget;
	onOpenWorkingChanges: () => void;
	onCreateBranchFromReflog: (entry: GitReflogEntry) => void;
	onBranchCreationClose: () => void;
	onCurrentBranchNameChange: (branchName: string) => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
	repositoryRefs: RepositoryRefsResponse | null;
	reflogController: ReflogManagementController;
	resetCommit: CommitGraphRow | null;
	revertCommit: CommitGraphRow | null;
	setCherryPickCommit: (commit: CommitGraphRow | null) => void;
	setIntegrationTarget: (target: IntegrationTarget) => void;
	setResetCommit: (commit: CommitGraphRow | null) => void;
	setRevertCommit: (commit: CommitGraphRow | null) => void;
}) {
	return (
		<>
			<CreateBranchDialog
				currentBranchName={currentBranchName}
				existingBranchNames={branchNames}
				onBranchChanged={(branchName) => {
					onCurrentBranchNameChange(branchName);
					onRepositoryChanged();
				}}
				onOpenChange={(open) => !open && onBranchCreationClose()}
				onRepositoryChanged={onRepositoryChanged}
				open={branchCreationSource !== null}
				repositoryId={repositoryId}
				source={branchCreationSource ?? undefined}
			/>
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
			{reflogController.branchName ? (
				<ReflogDialog
					branchName={reflogController.branchName}
					onClose={reflogController.close}
					onCreateBranch={(entry) => {
						reflogController.close();
						onCreateBranchFromReflog(entry);
					}}
					onReset={reflogController.startReset}
					repositoryId={repositoryId}
				/>
			) : null}
			{reflogController.resetTarget && currentBranchName ? (
				<ReflogResetDialog
					currentBranchName={currentBranchName}
					entry={reflogController.resetTarget}
					onClose={reflogController.closeReset}
					onOpenWorkingChanges={onOpenWorkingChanges}
					onRepositoryChanged={onRepositoryChanged}
					repositoryId={repositoryId}
				/>
			) : null}
		</>
	);
}
