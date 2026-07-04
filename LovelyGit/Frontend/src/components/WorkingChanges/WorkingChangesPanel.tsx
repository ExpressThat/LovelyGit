import { useMemo, useState } from "react";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { CommitStagedForm } from "./CommitStagedForm";
import { DiscardWorkingTreeChangesDialog } from "./DiscardWorkingTreeChangesDialog";
import { useWorkingChangesPanelActions } from "./useWorkingChangesPanelActions";
import { WorkingChangesFilterBar } from "./WorkingChangesFilterBar";
import {
	countWorkingChanges,
	filterWorkingChanges,
	type WorkingChangesFilterGroup,
} from "./WorkingChangesFilterUtils";
import { WorkingChangesGroups } from "./WorkingChangesGroups";
import {
	BulkIndexActions,
	WorkingChangesHeader,
} from "./WorkingChangesPanelParts";
export function WorkingChangesPanel({
	changes,
	error,
	isLoading,
	onRefresh,
	onCommitSuccess,
	onSelectFile,
	repositoryId,
	totalCount,
}: {
	changes: WorkingTreeChangesResponse | null;
	error: string | null;
	isLoading: boolean;
	onCommitSuccess: () => Promise<void> | void;
	onRefresh: () => Promise<void> | void;
	onSelectFile: (file: WorkingTreeChangedFile) => void;
	repositoryId: string;
	totalCount: number;
}) {
	const workingFiles = changes
		? [...changes.unstaged, ...changes.untracked]
		: [];
	const [filterQuery, setFilterQuery] = useState("");
	const [filterGroup, setFilterGroup] =
		useState<WorkingChangesFilterGroup>("All");
	const filteredChanges = useMemo(
		() =>
			changes
				? filterWorkingChanges(changes, {
						group: filterGroup,
						query: filterQuery,
					})
				: null,
		[changes, filterGroup, filterQuery],
	);
	const filteredCount = filteredChanges
		? countWorkingChanges(filteredChanges)
		: 0;
	const filteredWorkingFiles = filteredChanges
		? [...filteredChanges.unstaged, ...filteredChanges.untracked]
		: [];
	const {
		actionError,
		commitBody,
		commitStagedChanges,
		commitTitle,
		discardFiles,
		discardWorkingChanges,
		isBusy,
		isCommitting,
		isMutating,
		runIndexCommand,
		selectedKeys,
		setCommitBody,
		setCommitTitle,
		setDiscardFiles,
		toggleSelected,
	} = useWorkingChangesPanelActions({
		changes,
		onCommitSuccess,
		onRefresh,
		repositoryId,
	});
	if (!changes && isLoading) {
		return (
			<div className="space-y-4 p-4 text-left text-sm">
				<WorkingChangesHeader
					isLoading={isLoading}
					onRefresh={onRefresh}
					totalCount={totalCount}
				/>
				<div className="rounded-md border bg-card/70 p-4 text-sm text-muted-foreground">
					{totalCount === 0
						? "No working changes."
						: "Checking the working tree."}
				</div>
			</div>
		);
	}
	return (
		<div className="space-y-4 p-4 text-left text-sm">
			<WorkingChangesHeader
				isLoading={isLoading}
				onRefresh={onRefresh}
				totalCount={changes?.totalCount ?? totalCount}
			/>
			{changes && changes.totalCount > 0 ? (
				<BulkIndexActions
					canStage={workingFiles.length > 0}
					canUnstage={changes.staged.length > 0}
					isBusy={isBusy}
					onStageAll={() =>
						void runIndexCommand("StageWorkingTreeFiles", [], true)
					}
					onUnstageAll={() =>
						void runIndexCommand("UnstageWorkingTreeFiles", [], true)
					}
				/>
			) : null}
			{changes && changes.totalCount > 0 ? (
				<WorkingChangesFilterBar
					group={filterGroup}
					matchedCount={filteredCount}
					onGroupChange={setFilterGroup}
					onQueryChange={setFilterQuery}
					query={filterQuery}
					totalCount={changes.totalCount}
				/>
			) : null}
			{error || actionError ? (
				<div className="rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
					{actionError ?? error}
				</div>
			) : null}
			{changes && changes.totalCount === 0 ? (
				<div className="rounded-md border bg-card p-4 text-sm text-muted-foreground">
					No working changes.
				</div>
			) : null}
			{changes && changes.staged.length > 0 ? (
				<CommitStagedForm
					commitBody={commitBody}
					commitTitle={commitTitle}
					isBusy={isBusy}
					isCommitting={isCommitting}
					onCommit={() => void commitStagedChanges()}
					onCommitBodyChange={setCommitBody}
					onCommitTitleChange={setCommitTitle}
				/>
			) : null}
			{filteredChanges && filteredChanges.totalCount > 0 ? (
				<>
					{filteredCount > 0 ? (
						<WorkingChangesGroups
							isBusy={isBusy}
							onDiscardSelected={setDiscardFiles}
							onIndexCommand={(commandType, files, includeAll) =>
								void runIndexCommand(commandType, files, includeAll)
							}
							onSelectFile={onSelectFile}
							onToggleSelected={toggleSelected}
							selectedKeys={selectedKeys}
							stagedFiles={filteredChanges.staged}
							unmergedFiles={filteredChanges.unmerged}
							workingFiles={filteredWorkingFiles}
						/>
					) : (
						<div className="rounded-md border bg-card p-4 text-sm text-muted-foreground">
							No changes match this filter.
						</div>
					)}
					<DiscardWorkingTreeChangesDialog
						files={discardFiles}
						isDiscarding={isMutating}
						isOpen={discardFiles.length > 0}
						onConfirm={() => void discardWorkingChanges()}
						onOpenChange={(isOpen) => {
							if (!isOpen && !isMutating) {
								setDiscardFiles([]);
							}
						}}
					/>
				</>
			) : null}
		</div>
	);
}
