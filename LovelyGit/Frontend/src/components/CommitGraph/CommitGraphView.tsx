import type { CommitGraphViewProps } from "./CommitGraphViewProps";
import { BranchManagementDialogs } from "./components/BranchManagementDialogs";
import { CommitGraphHeader } from "./components/CommitGraphHeader";
import { CommitGraphHorizontalScrollbar } from "./components/CommitGraphHorizontalScrollbar";
import { CommitGraphOperationDialogs } from "./components/CommitGraphOperationDialogs";
import { CommitRow } from "./components/CommitRow";
import { RefsPanel } from "./components/RefsPanel";
import { TagManagementDialogs } from "./components/TagManagementDialogs";
import { WorktreeManagementDialogs } from "./components/WorktreeManagementDialogs";
import { ROW_HEIGHT } from "./constants";
import { useBranchCreation } from "./hooks/useBranchCreation";
import { useBranchWorktreeControllers } from "./hooks/useBranchWorktreeControllers";
import { useCommitGraphData } from "./hooks/useCommitGraphData";
import {
	useCommitGraphDialogs,
	useNotifyCurrentBranch,
} from "./hooks/useCommitGraphDialogs";
import { useCommitGraphViewport } from "./hooks/useCommitGraphViewport";
import { useCommitPatchActions } from "./hooks/useCommitPatchActions";
import { useRepositoryRefs } from "./hooks/useRepositoryRefs";
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
		rows,
		totalRows,
	} = useCommitGraphData(refreshToken);
	const repositoryRefs = useRepositoryRefs(repositoryId, refreshToken);
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
		onWorktreesChanged: repositoryRefs.refresh,
		remoteName: tagRemoteName,
		repositoryId,
	});
	const branchCreation = useBranchCreation();
	const { busyTag, deleteTag, deleteTagName, manageTag, setDeleteTagName } =
		tagController;
	useNotifyCurrentBranch(currentBranchName, onCurrentBranchNameChange);
	const {
		graphContentWidth,
		graphScrollerRef,
		graphScrollLeft,
		handleResizeStart,
		scrollRef,
		setGraphScrollLeft,
		templateColumns,
		virtualItems,
		virtualizer,
		viewportRef,
	} = useCommitGraphViewport({ ensureRangeLoaded, laneCount, totalRows });
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
						onTagAction={manageTag}
						remotePrefixes={remotePrefixes}
						repositoryRefs={repositoryRefs.refs}
						rows={rows}
						tagMutationBusy={busyTag !== null}
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
								style={{ height: `${virtualizer.getTotalSize()}px` }}
							>
								{virtualItems.map((item) => (
									<div
										className="absolute left-0 top-0 w-full"
										key={item.key}
										style={{
											height: `${ROW_HEIGHT}px`,
											transform: `translateY(${Math.round(item.start)}px)`,
										}}
									>
										<CommitRow
											branchMutationBusy={branchController.busyBranch !== null}
											branchRemoteName={tagRemoteName}
											comparison={dialogs.comparison}
											copyPatchBusy={
												patchActions.busyAction === "copy" &&
												patchActions.busyCommitHash ===
													rows[item.index]?.commit.hash
											}
											savePatchBusy={
												patchActions.busyAction === "save" &&
												patchActions.busyCommitHash ===
													rows[item.index]?.commit.hash
											}
											graph={{
												contentWidth: graphContentWidth,
												scrollLeft: graphScrollLeft,
											}}
											isSelected={
												Boolean(rows[item.index]) &&
												rows[item.index]?.commit.hash === selectedCommitHash
											}
											isHead={rows[item.index]?.commit.hash === currentHeadHash}
											onCherryPick={dialogs.setCherryPickCommit}
											onCheckoutCommit={dialogs.setCheckoutCommit}
											onBranchAction={manageBranch}
											onCreateBranch={branchCreation.createAtCommit}
											onCopyPatch={patchActions.copyPatch}
											onSavePatch={patchActions.savePatch}
											onCreateBranchFromTag={branchCreation.createFromTag}
											onCreateTag={dialogs.setTagCommit}
											onIntegrateBranch={dialogs.integrateBranch}
											onInteractiveRebase={dialogs.setInteractiveRebaseBase}
											onRevert={dialogs.setRevertCommit}
											onReset={dialogs.setResetCommit}
											onSelect={onSelectCommit}
											onTagAction={manageTag}
											currentBranchName={currentBranchName}
											remotePrefixes={remotePrefixes}
											row={rows[item.index] ?? null}
											rowIndex={item.index}
											templateColumns={templateColumns}
											tagMutationBusy={busyTag !== null}
											tagRemoteName={tagRemoteName}
										/>
									</div>
								))}
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
				cherryPickCommit={dialogs.cherryPickCommit}
				checkoutCommit={dialogs.checkoutCommit}
				commitComparison={dialogs.comparison}
				currentBranchName={currentBranchName}
				integrationTarget={dialogs.integrationTarget}
				interactiveRebaseBase={dialogs.interactiveRebaseBase}
				onCreateBranchFromReflog={branchCreation.createFromReflog}
				onOpenWorkingChanges={onOpenWorkingChanges}
				onBranchCreationClose={branchCreation.close}
				onCurrentBranchNameChange={(name) => onCurrentBranchNameChange?.(name)}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId={repositoryId}
				repositoryRefs={repositoryRefs.refs}
				reflogController={reflogController}
				resetCommit={dialogs.resetCommit}
				revertCommit={dialogs.revertCommit}
				setCherryPickCommit={dialogs.setCherryPickCommit}
				setCheckoutCommit={dialogs.setCheckoutCommit}
				setIntegrationTarget={dialogs.setIntegrationTarget}
				setInteractiveRebaseBase={dialogs.setInteractiveRebaseBase}
				setResetCommit={dialogs.setResetCommit}
				setRevertCommit={dialogs.setRevertCommit}
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
				busyTag={busyTag}
				deleteTagName={deleteTagName}
				existingTagNames={existingTagNames}
				onCreateOpenChange={dialogs.setTagCommit}
				onDelete={() => void deleteTag()}
				onDeleteOpenChange={setDeleteTagName}
				onRepositoryChanged={onRepositoryChanged}
				remoteName={tagRemoteName}
				repositoryId={repositoryId}
				tagCommit={dialogs.tagCommit}
			/>
			<WorktreeManagementDialogs
				controller={worktreeController}
				worktrees={repositoryRefs.refs?.worktrees ?? []}
			/>
		</>
	);
}
