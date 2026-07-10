import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { CommitStagedForm } from "./CommitStagedForm";
import { DiscardWorkingTreeChangesDialog } from "./DiscardWorkingTreeChangesDialog";
import { RepositoryOperationBanner } from "./RepositoryOperationBanner";
import { StashDialog } from "./StashDialog";
import { useWorkingChangesPanelActions } from "./useWorkingChangesPanelActions";
import { splitWorkingChanges, WorkingChangesList } from "./WorkingChangesList";
import { WorkingChangesHeader } from "./WorkingChangesPanelParts";
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
	const workingFiles = changes
		? [...changes.unstaged, ...changes.untracked]
		: [];
	const visibleChanges = changes ?? {
		staged: [],
		unstaged: [],
		untracked: [],
		unmerged: [],
		totalCount,
	};
	const { stagedFiles, unstagedFiles } = splitWorkingChanges(visibleChanges);
	const {
		actionError,
		commitBody,
		commitStagedChanges,
		commitTitle,
		discardFiles,
		discardWorkingChanges,
		isBusy,
		isAmending,
		isCommitting,
		isLoadingAmendMessage,
		isMutating,
		runIndexCommand,
		selectedKeys,
		setCommitBody,
		setCommitTitle,
		setDiscardFiles,
		toggleSelected,
		toggleAmend,
	} = useWorkingChangesPanelActions({
		changes,
		onCommitSuccess,
		onRefresh,
		repositoryId,
	});
	return (
		<div className="flex h-full min-h-0 flex-col gap-4 p-4 text-left text-sm">
			<WorkingChangesHeader
				actions={
					<StashDialog
						canCreate={(changes?.totalCount ?? totalCount) > 0}
						onRepositoryChanged={onCommitSuccess}
						repositoryId={repositoryId}
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
			<div className="min-h-0 flex-1">
				<WorkingChangesList
					isBusy={isBusy}
					isLoading={!changes && isLoading}
					onDiscardAll={() => setDiscardFiles(workingFiles)}
					onDiscardSelected={setDiscardFiles}
					onIndexCommand={(commandType, files, includeAll) =>
						void runIndexCommand(commandType, files, includeAll)
					}
					onOpenFileHistory={onOpenFileHistory}
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
				isBusy={isBusy}
				isAmending={isAmending}
				isCommitting={isCommitting}
				isLoadingAmendMessage={isLoadingAmendMessage}
				onAmendChange={(enabled) => void toggleAmend(enabled)}
				onCommit={() => void commitStagedChanges()}
				onCommitBodyChange={setCommitBody}
				onCommitTitleChange={setCommitTitle}
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
