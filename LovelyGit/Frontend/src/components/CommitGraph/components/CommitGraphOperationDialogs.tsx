import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { BranchCreationSource } from "@/components/TopNavBar/components/CreateBranchDialog";
import {
	LazyBranchIntegrationDialog,
	LazyCreateBranchDialog,
} from "@/components/TopNavBar/components/LazyRepositoryDialogs";
import type {
	CommitGraphRow,
	GitReflogEntry,
	RepositoryRefsResponse,
} from "@/generated/types";
import type { CommitComparisonController } from "../hooks/useCommitGraphDialogs";
import type { ReflogManagementController } from "../hooks/useReflogManagement";
import {
	LazyCheckoutCommitDialog,
	LazyCherryPickDialog,
	LazyInteractiveRebaseDialog,
	LazyReflogDialog,
	LazyReflogResetDialog,
	LazyResetCommitDialog,
	LazyRevertDialog,
} from "./LazyCommitOperationDialogs";
import { LazyCommitComparisonDialog } from "./LazyGraphManagementDialogs";

type IntegrationTarget = {
	branchName: string;
	mode: BranchIntegrationMode;
} | null;

export function CommitGraphOperationDialogs({
	cherryPickCommit,
	checkoutCommit,
	commitComparison,
	branchCreationSource,
	branchNames,
	currentBranchName,
	integrationTarget,
	interactiveRebaseBase,
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
	setCheckoutCommit,
	setIntegrationTarget,
	setInteractiveRebaseBase,
	setResetCommit,
	setRevertCommit,
}: {
	cherryPickCommit: CommitGraphRow | null;
	checkoutCommit: CommitGraphRow | null;
	commitComparison: CommitComparisonController;
	branchCreationSource: BranchCreationSource | null;
	branchNames: string[];
	currentBranchName: string | null;
	integrationTarget: IntegrationTarget;
	interactiveRebaseBase: CommitGraphRow | null;
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
	setCheckoutCommit: (commit: CommitGraphRow | null) => void;
	setIntegrationTarget: (target: IntegrationTarget) => void;
	setInteractiveRebaseBase: (commit: CommitGraphRow | null) => void;
	setResetCommit: (commit: CommitGraphRow | null) => void;
	setRevertCommit: (commit: CommitGraphRow | null) => void;
}) {
	return (
		<>
			{commitComparison.base && commitComparison.target ? (
				<LazyCommitComparisonDialog
					base={commitComparison.base}
					onClose={() => commitComparison.setTarget(null)}
					repositoryId={repositoryId}
					target={commitComparison.target}
				/>
			) : null}
			{checkoutCommit ? (
				<LazyCheckoutCommitDialog
					commit={checkoutCommit}
					onClose={() => setCheckoutCommit(null)}
					onRepositoryChanged={onRepositoryChanged}
					repositoryId={repositoryId}
				/>
			) : null}
			<LazyCreateBranchDialog
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
			<LazyBranchIntegrationDialog
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
			<LazyCherryPickDialog
				commit={cherryPickCommit}
				currentBranchName={currentBranchName}
				onOpenChange={setCherryPickCommit}
				onOpenWorkingChanges={onOpenWorkingChanges}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId={repositoryId}
			/>
			<LazyRevertDialog
				commit={revertCommit}
				currentBranchName={currentBranchName}
				onOpenChange={setRevertCommit}
				onOpenWorkingChanges={onOpenWorkingChanges}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId={repositoryId}
			/>
			<LazyResetCommitDialog
				commit={resetCommit}
				currentBranchName={currentBranchName}
				onOpenChange={setResetCommit}
				onOpenWorkingChanges={onOpenWorkingChanges}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId={repositoryId}
			/>
			<LazyInteractiveRebaseDialog
				baseCommit={interactiveRebaseBase}
				currentBranchName={currentBranchName}
				onOpenChange={setInteractiveRebaseBase}
				onOpenWorkingChanges={onOpenWorkingChanges}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId={repositoryId}
			/>
			{reflogController.branchName ? (
				<LazyReflogDialog
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
				<LazyReflogResetDialog
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
