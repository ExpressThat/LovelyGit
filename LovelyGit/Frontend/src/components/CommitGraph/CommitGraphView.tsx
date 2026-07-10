import { useEffect, useState } from "react";
import {
	BranchIntegrationDialog,
	type BranchIntegrationMode,
} from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { CommitGraphRow as CommitGraphRowModel } from "@/generated/types";
import { BranchManagementDialogs } from "./components/BranchManagementDialogs";
import { CherryPickDialog } from "./components/CherryPickDialog";
import { CommitGraphHeader } from "./components/CommitGraphHeader";
import { CommitRow } from "./components/CommitRow";
import { RefsPanel } from "./components/RefsPanel";
import { RevertDialog } from "./components/RevertDialog";
import { TagManagementDialogs } from "./components/TagManagementDialogs";
import { ROW_HEIGHT } from "./constants";
import { useBranchMutations } from "./hooks/useBranchMutations";
import { useCommitGraphData } from "./hooks/useCommitGraphData";
import { useCommitGraphViewport } from "./hooks/useCommitGraphViewport";
import { useRepositoryRefs } from "./hooks/useRepositoryRefs";
import { useTagMutations } from "./hooks/useTagMutations";
import { refNames } from "./utils/refMetadata";

export function CommitGraphView({
	onCurrentBranchNameChange,
	onOpenWorkingChanges,
	onRepositoryChanged,
	onSelectCommit,
	refreshToken = 0,
	repositoryId,
	selectedCommitHash,
}: {
	onCurrentBranchNameChange?: (branchName: string | null) => void;
	onOpenWorkingChanges: () => void;
	onRepositoryChanged: () => void;
	onSelectCommit: (row: CommitGraphRowModel) => void;
	refreshToken?: number;
	repositoryId: string | null;
	selectedCommitHash: string | null;
}) {
	const [integrationTarget, setIntegrationTarget] = useState<{
		branchName: string;
		mode: BranchIntegrationMode;
	} | null>(null);
	const [cherryPickCommit, setCherryPickCommit] =
		useState<CommitGraphRowModel | null>(null);
	const [revertCommit, setRevertCommit] = useState<CommitGraphRowModel | null>(
		null,
	);
	const [tagCommit, setTagCommit] = useState<CommitGraphRowModel | null>(null);

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
	const tagRemoteName =
		repositoryRefs.refs?.remotePrefixes[0] ?? remotePrefixes[0] ?? null;
	const existingTagNames = refNames(repositoryRefs.refs, "Tag");
	const branchNames = refNames(repositoryRefs.refs, "Local");
	const branchController = useBranchMutations({
		currentBranchName,
		onCurrentBranchNameChange: (name) => onCurrentBranchNameChange?.(name),
		onRepositoryChanged,
		remoteName: tagRemoteName,
		repositoryId,
	});
	const { busyTag, deleteTag, deleteTagName, pushTag, setDeleteTagName } =
		useTagMutations({
			onRepositoryChanged,
			remoteName: tagRemoteName,
			repositoryId,
		});
	const manageTag = (action: "delete" | "push", tagName: string) => {
		if (action === "push") void pushTag(tagName);
		else setDeleteTagName(tagName);
	};
	const currentHeadHash =
		repositoryRefs.refs?.refs.find(
			(ref) => ref.kind === "Local" && ref.name === currentBranchName,
		)?.commitHash ?? null;

	useEffect(() => {
		onCurrentBranchNameChange?.(currentBranchName);
	}, [currentBranchName, onCurrentBranchNameChange]);

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

	const integrateBranch = (mode: BranchIntegrationMode, branchName: string) =>
		setIntegrationTarget({ branchName, mode });

	return (
		<>
			<section className="h-full w-full overflow-hidden bg-background">
				<div className="flex h-full w-full min-w-0">
					<RefsPanel
						branchMutationBusy={branchController.busyBranch !== null}
						branchRemoteName={tagRemoteName}
						currentBranchName={currentBranchName}
						onBranchAction={branchController.manageBranch}
						onIntegrateBranch={integrateBranch}
						onSelectCommit={onSelectCommit}
						onTagAction={manageTag}
						remotePrefixes={remotePrefixes}
						repositoryRefs={repositoryRefs.refs}
						rows={rows}
						tagMutationBusy={busyTag !== null}
						tagRemoteName={tagRemoteName}
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
											graph={{
												contentWidth: graphContentWidth,
												scrollLeft: graphScrollLeft,
											}}
											isSelected={
												Boolean(rows[item.index]) &&
												rows[item.index]?.commit.hash === selectedCommitHash
											}
											isHead={rows[item.index]?.commit.hash === currentHeadHash}
											onCherryPick={setCherryPickCommit}
											onBranchAction={branchController.manageBranch}
											onCreateTag={setTagCommit}
											onIntegrateBranch={integrateBranch}
											onRevert={setRevertCommit}
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
						<div
							className="grid h-3 border-t bg-background"
							style={{ gridTemplateColumns: templateColumns }}
						>
							<div />
							<div
								className="custom-scrollbar overflow-x-auto overflow-y-hidden"
								ref={graphScrollerRef}
								onScroll={(event) =>
									setGraphScrollLeft(event.currentTarget.scrollLeft)
								}
							>
								<div style={{ height: 1, width: graphContentWidth }} />
							</div>
							<div />
							<div />
							<div />
						</div>
					</div>
				</div>
			</section>
			<BranchIntegrationDialog
				branches={(repositoryRefs.refs?.refs ?? []).filter(
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
			<BranchManagementDialogs
				branchNames={branchNames}
				controller={branchController}
			/>
			<TagManagementDialogs
				busyTag={busyTag}
				deleteTagName={deleteTagName}
				existingTagNames={existingTagNames}
				onCreateOpenChange={setTagCommit}
				onDelete={() => void deleteTag()}
				onDeleteOpenChange={setDeleteTagName}
				onRepositoryChanged={onRepositoryChanged}
				remoteName={tagRemoteName}
				repositoryId={repositoryId}
				tagCommit={tagCommit}
			/>
		</>
	);
}
