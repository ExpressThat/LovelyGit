import type { BranchCreationSource } from "@/components/TopNavBar/components/CreateBranchDialog";
import {
	LazyBranchIntegrationDialog,
	LazyCreateBranchDialog,
} from "@/components/TopNavBar/components/LazyRepositoryDialogs";
import type { GitReflogEntry, RepositoryRefsResponse } from "@/generated/types";
import type { useCommitGraphDialogs } from "../hooks/useCommitGraphDialogs";
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
import { StartBisectDialog } from "./StartBisectDialog";

export function CommitGraphOperationDialogs({
	branchCreationSource,
	branchNames,
	currentBranchName,
	dialogs,
	onOpenWorkingChanges,
	onCreateBranchFromReflog,
	onBranchCreationClose,
	onCurrentBranchNameChange,
	onRepositoryChanged,
	repositoryId,
	repositoryRefs,
	reflogController,
}: {
	branchCreationSource: BranchCreationSource | null;
	branchNames: string[];
	currentBranchName: string | null;
	dialogs: ReturnType<typeof useCommitGraphDialogs>;
	onOpenWorkingChanges: () => void;
	onCreateBranchFromReflog: (entry: GitReflogEntry) => void;
	onBranchCreationClose: () => void;
	onCurrentBranchNameChange: (branchName: string) => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
	repositoryRefs: RepositoryRefsResponse | null;
	reflogController: ReflogManagementController;
}) {
	const {
		bisectCommit,
		cherryPickCommits,
		checkoutCommit,
		comparison: commitComparison,
		integrationTarget,
		interactiveRebaseBase,
		resetCommit,
		revertCommits,
		setBisectCommit,
		setCherryPickCommits,
		setCheckoutCommit,
		setIntegrationTarget,
		setInteractiveRebaseBase,
		setResetCommit,
		setRevertCommits,
	} = dialogs;
	return (
		<>
			{bisectCommit ? (
				<StartBisectDialog
					commit={bisectCommit}
					onOpenChange={(open) => !open && setBisectCommit(null)}
					repositoryId={repositoryId}
				/>
			) : null}
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
				commits={cherryPickCommits}
				currentBranchName={currentBranchName}
				onOpenChange={setCherryPickCommits}
				onOpenWorkingChanges={onOpenWorkingChanges}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId={repositoryId}
			/>
			<LazyRevertDialog
				commits={revertCommits}
				currentBranchName={currentBranchName}
				onOpenChange={setRevertCommits}
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
