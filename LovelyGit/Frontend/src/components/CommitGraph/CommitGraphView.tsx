import { CommitGraphRows } from "./CommitGraphRows";
import type { CommitGraphViewProps } from "./CommitGraphViewProps";
import { BranchManagementDialogs } from "./components/BranchManagementDialogs";
import { CommitGraphHeader } from "./components/CommitGraphHeader";
import { CommitGraphHorizontalScrollbar } from "./components/CommitGraphHorizontalScrollbar";
import { CommitGraphOperationDialogs } from "./components/CommitGraphOperationDialogs";
import { CommitMultiSelectionBar } from "./components/CommitMultiSelectionBar";
import { RefsPanel } from "./components/RefsPanel";
import { TagManagementDialogs } from "./components/TagManagementDialogs";
import { WorktreeManagementDialogs } from "./components/WorktreeManagementDialogs";
import { useBranchCreation } from "./hooks/useBranchCreation";
import { useBranchWorktreeControllers } from "./hooks/useBranchWorktreeControllers";
import { useCommitGraphData } from "./hooks/useCommitGraphData";
import {
	useCommitGraphDialogs,
	useNotifyCurrentBranch,
} from "./hooks/useCommitGraphDialogs";
import { useCommitGraphViewport } from "./hooks/useCommitGraphViewport";
import { useCommitMultiSelection } from "./hooks/useCommitMultiSelection";
import { useCommitPatchActions } from "./hooks/useCommitPatchActions";
import { useRepositoryRefs } from "./hooks/useRepositoryRefs";
import { createSelectedCommitActions } from "./hooks/useSelectedCommitActions";
import { buildCommitGraphRefView } from "./utils/commitGraphRefView";
export function CommitGraphView({
	onCurrentBranchNameChange,
	onOpenWorkingChanges,
	onRepositoryChanged,
	onSelectCommit,
	refreshToken = 0,
	repositoryId,
	selectedCommitHash,
}: CommitGraphViewProps) {
	const dialogs = useCommitGraphDialogs();
	const patchActions = useCommitPatchActions(repositoryId);
	const {
		currentBranchName,
		ensureRangeLoaded,
		error,
		isInitialLoading,
		laneCount,
		remotePrefixes,
		refRowsByHash,
		rows,
		totalRows,
	} = useCommitGraphData(refreshToken);
	const repositoryRefs = useRepositoryRefs(repositoryId, refreshToken);
	const commitSelection = useCommitMultiSelection(repositoryId, rows);
	const selectedActions = createSelectedCommitActions({
		dialogs,
		patchActions,
		selection: commitSelection,
	});
	const {
		branchNames,
		branchUpstreams,
		currentHeadHash,
		existingTagNames,
		remoteBranchNames,
		tagRemoteName,
	} = buildCommitGraphRefView(
		repositoryRefs.refs,
		remotePrefixes,
		currentBranchName,
	);
	const {
		branchController,
		manageBranch,
		reflogController,
		tagController,
		worktreeController,
	} = useBranchWorktreeControllers({
		currentBranchName,
		onCurrentBranchNameChange: (name) => onCurrentBranchNameChange?.(name),
		onRepositoryChanged,
		onUpstreamChanged: repositoryRefs.updateBranchUpstream,
		onWorktreeLockChanged: repositoryRefs.updateWorktreeLock,
		onWorktreeRemoved: repositoryRefs.removeWorktree,
		onWorktreesChanged: repositoryRefs.refresh,
		remoteName: tagRemoteName,
		repositoryId,
	});
	const branchCreation = useBranchCreation();
	useNotifyCurrentBranch(currentBranchName, onCurrentBranchNameChange);
	const {
		contentHeight,
		graphContentWidth,
		graphScrollerRef,
		graphScrollLeft,
		handleResizeStart,
		scrollRef,
		setGraphScrollLeft,
		templateColumns,
		virtualItems,
		viewportRef,
	} = useCommitGraphViewport({
		ensureRangeLoaded,
		laneCount,
		repositoryId,
		totalRows,
	});
	return (
		<>
			<section className="h-full w-full overflow-hidden bg-background">
				<div className="flex h-full w-full min-w-0">
					<RefsPanel
						branchMutationBusy={branchController.busyBranch !== null}
						branchRemoteName={tagRemoteName}
						currentBranchName={currentBranchName}
						onBranchAction={manageBranch}
						onCreateBranchFromTag={branchCreation.createFromTag}
						onIntegrateBranch={dialogs.integrateBranch}
						onSelectCommit={onSelectCommit}
						onTagAction={tagController.manageTag}
						remotePrefixes={remotePrefixes}
						refRowsByHash={refRowsByHash}
						repositoryRefs={repositoryRefs.refs}
						rows={rows}
						tagMutationBusy={tagController.busyTag !== null}
						tagRemoteName={tagRemoteName}
						worktreeController={worktreeController}
					/>
					<div
						ref={viewportRef}
						className="flex h-full min-w-0 flex-1 flex-col"
					>
						<CommitGraphHeader
							isInitialLoading={isInitialLoading}
							onResizeStart={handleResizeStart}
							templateColumns={templateColumns}
						/>
						<CommitMultiSelectionBar
							cherryPickDisabled={
								currentBranchName === null ||
								Boolean(
									currentHeadHash &&
										commitSelection.hashes.has(currentHeadHash),
								)
							}
							count={commitSelection.count}
							onCherryPick={() => selectedActions.openOperation("cherry-pick")}
							onClear={commitSelection.clear}
							onCompare={selectedActions.openComparison}
							onCopyPatchSeries={() => selectedActions.runPatchSeries("copy")}
							onRevert={() => selectedActions.openOperation("revert")}
							onSavePatchSeries={() => selectedActions.runPatchSeries("save")}
							revertDisabled={currentBranchName === null}
							seriesBusyAction={patchActions.seriesBusyAction}
						/>
						{error ? (
							<div className="h-7 border-b border-destructive/40 bg-destructive/10 px-[10px] leading-[27px] text-destructive">
								{error}
							</div>
						) : null}

						<div
							ref={scrollRef}
							className="custom-scrollbar relative min-h-0 w-full flex-1 overflow-x-hidden overflow-y-auto bg-[repeating-linear-gradient(to_bottom,var(--background)_0,var(--background)_21px,var(--card)_21px,var(--card)_22px)]"
						>
							<div
								className="relative h-full w-full"
								style={{ height: `${contentHeight}px` }}
							>
								<CommitGraphRows
									actions={{
										branchController,
										branchCreation,
										dialogs,
										manageBranch,
										onSelectCommit,
										patchActions,
										tagController,
										virtualItems,
									}}
									rows={rows}
									selection={commitSelection}
									view={{
										currentBranchName,
										currentHeadHash,
										graphContentWidth,
										graphScrollLeft,
										remotePrefixes,
										repositoryId,
										selectedCommitHash,
										tagRemoteName,
										templateColumns,
									}}
								/>
							</div>
						</div>
						<CommitGraphHorizontalScrollbar
							contentWidth={graphContentWidth}
							onScrollLeftChange={setGraphScrollLeft}
							scrollerRef={graphScrollerRef}
							templateColumns={templateColumns}
						/>
					</div>
				</div>
			</section>
			<CommitGraphOperationDialogs
				branchCreationSource={branchCreation.source}
				branchNames={branchNames}
				currentBranchName={currentBranchName}
				dialogs={dialogs}
				onCreateBranchFromReflog={branchCreation.createFromReflog}
				onOpenWorkingChanges={onOpenWorkingChanges}
				onBranchCreationClose={branchCreation.close}
				onCurrentBranchNameChange={(name) => onCurrentBranchNameChange?.(name)}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId={repositoryId}
				repositoryRefs={repositoryRefs.refs}
				reflogController={reflogController}
			/>
			<BranchManagementDialogs
				branchNames={branchNames}
				controller={branchController}
				currentBranchName={currentBranchName}
				onIntegrateBranch={dialogs.integrateBranch}
				remoteBranches={remoteBranchNames}
				repositoryId={repositoryId}
				upstreams={branchUpstreams}
			/>
			<TagManagementDialogs
				controller={tagController}
				existingTagNames={existingTagNames}
				onCreateOpenChange={dialogs.setTagCommit}
				onRepositoryChanged={onRepositoryChanged}
				remoteName={tagRemoteName}
				repositoryId={repositoryId}
				tagCommit={dialogs.tagCommit}
			/>
			<WorktreeManagementDialogs
				controller={worktreeController}
				repositoryRefs={repositoryRefs.refs}
			/>
		</>
	);
}
