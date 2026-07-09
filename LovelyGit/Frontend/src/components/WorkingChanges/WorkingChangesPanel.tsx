import { CheckCircle2 } from "lucide-react";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { CommitStagedForm } from "./CommitStagedForm";
import { DiscardWorkingTreeChangesDialog } from "./DiscardWorkingTreeChangesDialog";
import { useWorkingChangesPanelActions } from "./useWorkingChangesPanelActions";
import { splitWorkingChanges, WorkingChangesList } from "./WorkingChangesList";
import { WorkingChangesHeader } from "./WorkingChangesPanelParts";
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
		isCommitting,
		isMutating,
		runIndexCommand,
		selectedKeys,
		setCommitBody,
		setCommitTitle,
		setDiscardFiles,
		successMessage,
		toggleSelected,
	} = useWorkingChangesPanelActions({
		changes,
		onCommitSuccess,
		onRefresh,
		repositoryId,
	});
	return (
		<div className="flex h-full min-h-0 flex-col gap-4 p-4 text-left text-sm">
			<WorkingChangesHeader
				isLoading={isLoading}
				onRefresh={onRefresh}
				totalCount={changes?.totalCount ?? totalCount}
			/>
			{error || actionError ? (
				<div className="rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
					{actionError ?? error}
				</div>
			) : null}
			{successMessage ? (
				<div className="flex items-center gap-2 rounded-md border border-primary/30 bg-primary/10 p-3 text-sm text-foreground">
					<CheckCircle2 aria-hidden="true" className="text-primary" size={16} />
					<span>{successMessage}</span>
				</div>
			) : null}
			<div className="min-h-0 flex-1">
				<WorkingChangesList
					isBusy={isBusy}
					isLoading={!changes && isLoading}
					onDiscardAll={() => setDiscardFiles(workingFiles)}
					onDiscardSelected={setDiscardFiles}
					onIndexCommand={(commandType, files, includeAll) =>
						void runIndexCommand(commandType, files, includeAll)
					}
					onSelectFile={onSelectFile}
					onToggleSelected={toggleSelected}
					selectedKeys={selectedKeys}
					stagedFiles={stagedFiles}
					unstagedFiles={unstagedFiles}
					workingFiles={workingFiles}
				/>
			</div>
			<CommitStagedForm
				canCommit={stagedFiles.length > 0}
				commitBody={commitBody}
				commitTitle={commitTitle}
				isBusy={isBusy}
				isCommitting={isCommitting}
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
