import { useState } from "react";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { CommitStagedForm } from "./CommitStagedForm";
import { DiscardWorkingTreeChangesDialog } from "./DiscardWorkingTreeChangesDialog";
import { RepositoryOperationBanner } from "./RepositoryOperationBanner";
import { StashDialog } from "./StashDialog";
import { UndoLastCommitDialog } from "./UndoLastCommitDialog";
import { useUndoLastCommit } from "./useUndoLastCommit";
import { useWorkingChangesPanelActions } from "./useWorkingChangesPanelActions";
import {
	splitWorkingChanges,
	WorkingChangesList,
	workingFilesOnly,
} from "./WorkingChangesList";
import {
	selectedStashPaths,
	WorkingChangesHeader,
} from "./WorkingChangesPanelParts";
export function WorkingChangesPanel({
	changes,
	error,
	isLoading,
	onRefresh,
	onCommitSuccess,
	onOpenFileBlame,
	onOpenFileHistory,
	onSelectFile,
	repositoryId,
	totalCount,
}: {
	changes: WorkingTreeChangesResponse | null;
	error: string | null;
	isLoading: boolean;
	onCommitSuccess: () => Promise<void> | void;
	onRefresh: () => Promise<void> | void;
	onOpenFileBlame: (file: WorkingTreeChangedFile) => void;
	onOpenFileHistory: (file: WorkingTreeChangedFile) => void;
	onSelectFile: (file: WorkingTreeChangedFile) => void;
	repositoryId: string;
	totalCount: number;
}) {
	const [optimisticView, setOptimisticView] =
		useState<OptimisticWorkingTreeView | null>(null);
	const optimisticChanges =
		optimisticView?.repositoryId === repositoryId
			? optimisticView.changes
			: null;
	const setOptimisticChanges = (
		nextChanges: WorkingTreeChangesResponse | null,
	) =>
		setOptimisticView(
			nextChanges ? { changes: nextChanges, repositoryId } : null,
		);
	const visibleChanges = optimisticChanges ??
		changes ?? {
			staged: [],
			unstaged: [],
			untracked: [],
			unmerged: [],
			totalCount,
		};
	const { stagedFiles, unstagedFiles } = splitWorkingChanges(visibleChanges);
	const workingFiles = workingFilesOnly(unstagedFiles);
	const {
		actionError,
		commitBody,
		commitStagedChanges,
		commitTitle,
		discardFiles,
		discardWorkingChanges,
		isBusy,
		ignorePath,
		isAmending,
		isCommitting,
		isLoadingAmendMessage,
		isSigningCommit,
		isMutating,
		runIndexCommand,
		restoreCommitDraft,
		selectedKeys,
		setCommitBody,
		setCommitTitle,
		setDiscardFiles,
		setIsSigningCommit,
		toggleSelected,
		toggleAmend,
	} = useWorkingChangesPanelActions({
		changes: visibleChanges,
		onCommitSuccess,
		onRefresh,
		repositoryId,
		setOptimisticChanges,
	});
	const undo = useUndoLastCommit({
		onSuccess: async (message) => {
			restoreCommitDraft(message.title, message.body);
			await onCommitSuccess();
		},
		repositoryId,
	});
	const controlsBusy = isBusy || undo.isBusy;
	const stashPaths = selectedStashPaths(visibleChanges, selectedKeys);
	return (
		<div className="custom-scrollbar flex h-full min-h-0 flex-col gap-4 overflow-y-auto p-4 text-left text-sm">
			<WorkingChangesHeader
				actions={
					<StashDialog
						canCreate={(changes?.totalCount ?? totalCount) > 0}
						onRepositoryChanged={onCommitSuccess}
						repositoryId={repositoryId}
						selectedPaths={stashPaths}
					/>
				}
				isLoading={isLoading}
				onRefresh={onRefresh}
				totalCount={changes?.totalCount ?? totalCount}
			/>
			{error || actionError ? (
				<div className="rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
					{actionError ?? error}
				</div>
			) : null}
			<RepositoryOperationBanner
				conflictCount={visibleChanges.unmerged.length}
				onRefresh={onRefresh}
				onRepositoryChanged={onCommitSuccess}
				repositoryId={repositoryId}
				workingTreeCount={visibleChanges.totalCount}
			/>
			<div className="min-h-56 flex-1 shrink-0">
				<WorkingChangesList
					isBusy={controlsBusy}
					isLoading={!changes && isLoading}
					onDiscardAll={() => setDiscardFiles(workingFiles)}
					onDiscardSelected={setDiscardFiles}
					onIndexCommand={(commandType, files, includeAll) =>
						void runIndexCommand(commandType, files, includeAll)
					}
					onOpenFileHistory={onOpenFileHistory}
					onIgnorePath={(file, target) => void ignorePath(file.path, target)}
					onOpenFileBlame={onOpenFileBlame}
					onSelectFile={onSelectFile}
					onToggleSelected={toggleSelected}
					selectedKeys={selectedKeys}
					stagedFiles={stagedFiles}
					unstagedFiles={unstagedFiles}
					workingFiles={workingFiles}
				/>
			</div>
			<CommitStagedForm
				canCommit={stagedFiles.length > 0 || isAmending}
				commitBody={commitBody}
				commitTitle={commitTitle}
				isBusy={controlsBusy}
				isAmending={isAmending}
				isCommitting={isCommitting}
				isLoadingAmendMessage={isLoadingAmendMessage}
				isSigningCommit={isSigningCommit}
				onAmendChange={(enabled) => void toggleAmend(enabled)}
				onCommit={() => void commitStagedChanges()}
				onCommitBodyChange={setCommitBody}
				onCommitTitleChange={setCommitTitle}
				onSigningChange={setIsSigningCommit}
				onUndo={() => void undo.open()}
				repositoryId={repositoryId}
			/>
			<UndoLastCommitDialog
				error={undo.error}
				isLoading={undo.isLoading}
				isOpen={undo.isOpen}
				isUndoing={undo.isUndoing}
				onClose={undo.close}
				onConfirm={() => void undo.confirm()}
				preview={undo.preview}
			/>
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
		</div>
	);
}

type OptimisticWorkingTreeView = {
	changes: WorkingTreeChangesResponse;
	repositoryId: string;
};
