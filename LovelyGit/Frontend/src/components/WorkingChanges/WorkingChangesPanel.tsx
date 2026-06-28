import { useEffect, useMemo, useState } from "react";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { CommitStagedForm } from "./CommitStagedForm";
import { DiscardWorkingTreeChangesDialog } from "./DiscardWorkingTreeChangesDialog";
import { WorkingChangesGroups } from "./WorkingChangesGroups";
import {
	BulkIndexActions,
	fileKey,
	uniquePaths,
	WorkingChangesHeader,
	WorkingChangesSkeleton,
} from "./WorkingChangesPanelParts";
export function WorkingChangesPanel({
	changes,
	error,
	isLoading,
	onRefresh,
	onCommitSuccess,
	onSelectFile,
	repositoryId,
}: {
	changes: WorkingTreeChangesResponse | null;
	error: string | null;
	isLoading: boolean;
	onCommitSuccess: () => Promise<void> | void;
	onRefresh: () => Promise<void> | void;
	onSelectFile: (file: WorkingTreeChangedFile) => void;
	repositoryId: string;
}) {
	const workingFiles = changes
		? [...changes.unstaged, ...changes.untracked]
		: [];
	const [selectedKeys, setSelectedKeys] = useState<Set<string>>(
		() => new Set(),
	);
	const [actionError, setActionError] = useState<string | null>(null);
	const [isMutating, setIsMutating] = useState(false);
	const [commitTitle, setCommitTitle] = useState("");
	const [commitBody, setCommitBody] = useState("");
	const [isCommitting, setIsCommitting] = useState(false);
	const [discardFiles, setDiscardFiles] = useState<WorkingTreeChangedFile[]>(
		[],
	);
	const fileKeys = useMemo(
		() =>
			new Set(
				[
					...(changes?.staged ?? []),
					...(changes?.unstaged ?? []),
					...(changes?.untracked ?? []),
					...(changes?.unmerged ?? []),
				].map(fileKey),
			),
		[changes],
	);
	const isBusy = isMutating || isCommitting;
	useEffect(() => {
		setSelectedKeys((current) => {
			const next = new Set([...current].filter((key) => fileKeys.has(key)));
			return next.size === current.size ? current : next;
		});
	}, [fileKeys]);
	const runIndexCommand = async (
		commandType: "StageWorkingTreeFiles" | "UnstageWorkingTreeFiles",
		files: WorkingTreeChangedFile[],
		includeAll: boolean,
	) => {
		if (!includeAll && files.length === 0) {
			return;
		}
		setIsMutating(true);
		setActionError(null);
		try {
			await sendRequestWithResponse({
				commandType,
				arguments: {
					includeAll,
					paths: includeAll ? [] : uniquePaths(files),
					repositoryId,
				},
			});
			setSelectedKeys(new Set());
			await onRefresh();
		} catch (mutationError) {
			setActionError(
				mutationError instanceof Error
					? mutationError.message
					: "Failed to update the index.",
			);
		} finally {
			setIsMutating(false);
		}
	};
	const commitStagedChanges = async () => {
		if (
			!changes ||
			changes.staged.length === 0 ||
			commitTitle.trim().length === 0
		) {
			return;
		}
		setIsCommitting(true);
		setActionError(null);
		try {
			await sendRequestWithResponse({
				commandType: "CommitStagedChanges",
				arguments: {
					body: commitBody,
					repositoryId,
					title: commitTitle,
				},
			});
			setCommitTitle("");
			setCommitBody("");
			setSelectedKeys(new Set());
			await onCommitSuccess();
		} catch (commitError) {
			setActionError(
				commitError instanceof Error
					? commitError.message
					: "Failed to commit staged changes.",
			);
		} finally {
			setIsCommitting(false);
		}
	};
	const discardWorkingChanges = async () => {
		if (discardFiles.length === 0) {
			return;
		}
		setIsMutating(true);
		setActionError(null);
		try {
			await sendRequestWithResponse({
				commandType: "DiscardWorkingTreeChanges",
				arguments: {
					files: discardFiles,
					repositoryId,
				},
			});
			setDiscardFiles([]);
			setSelectedKeys(new Set());
			await onRefresh();
		} catch (discardError) {
			setActionError(
				discardError instanceof Error
					? discardError.message
					: "Failed to discard selected changes.",
			);
		} finally {
			setIsMutating(false);
		}
	};
	const toggleSelected = (file: WorkingTreeChangedFile) => {
		const key = fileKey(file);
		setSelectedKeys((current) => {
			const next = new Set(current);
			if (next.has(key)) {
				next.delete(key);
			} else {
				next.add(key);
			}
			return next;
		});
	};
	if (!changes && isLoading) {
		return <WorkingChangesSkeleton />;
	}
	return (
		<div className="space-y-4 p-4 text-left text-sm">
			<WorkingChangesHeader
				isLoading={isLoading}
				onRefresh={onRefresh}
				totalCount={changes?.totalCount ?? 0}
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
			{changes ? (
				<>
					<WorkingChangesGroups
						isBusy={isBusy}
						onDiscardSelected={setDiscardFiles}
						onIndexCommand={(commandType, files, includeAll) =>
							void runIndexCommand(commandType, files, includeAll)
						}
						onSelectFile={onSelectFile}
						onToggleSelected={toggleSelected}
						selectedKeys={selectedKeys}
						stagedFiles={changes.staged}
						unmergedFiles={changes.unmerged}
						workingFiles={workingFiles}
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
				</>
			) : null}
		</div>
	);
}
