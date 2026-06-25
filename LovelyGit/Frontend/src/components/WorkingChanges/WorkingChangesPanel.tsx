import { MinusSquare, RefreshCw, SquareCheckBig } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import type { WorkingTreeChangedFile, WorkingTreeChangesResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { CommitStagedForm } from "./CommitStagedForm";
import { ActionButton, ChangeGroup, fileKey, selectedFiles, uniquePaths } from "./WorkingChangesPanelParts";
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
	const workingFiles = changes ? [...changes.unstaged, ...changes.untracked] : [];
	const [selectedKeys, setSelectedKeys] = useState<Set<string>>(() => new Set());
	const [actionError, setActionError] = useState<string | null>(null);
	const [isMutating, setIsMutating] = useState(false);
	const [commitTitle, setCommitTitle] = useState("");
	const [commitBody, setCommitBody] = useState("");
	const [isCommitting, setIsCommitting] = useState(false);
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
	const selectedStagedFiles = selectedFiles(changes?.staged ?? [], selectedKeys);
	const selectedWorkingFiles = selectedFiles(workingFiles, selectedKeys);
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
		if (!changes || changes.staged.length === 0 || commitTitle.trim().length === 0) {
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
		return (
			<div className="space-y-3 p-4">
				<div className="h-4 w-36 animate-pulse rounded bg-muted" />
				<div className="h-24 animate-pulse rounded bg-muted" />
				<div className="h-32 animate-pulse rounded bg-muted" />
			</div>
		);
	}
	return (
		<div className="space-y-4 p-4 text-left text-sm">
			<div className="flex items-center justify-between gap-3">
				<div>
					<div className="font-semibold text-foreground">
						{changes?.totalCount ?? 0} changed files
					</div>
					<div className="text-xs text-muted-foreground">
						Staged, working tree, and unmerged
					</div>
				</div>
				<button
					aria-label="Refresh working changes"
					className="inline-flex size-7 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground"
					onClick={onRefresh}
					type="button"
				>
					<RefreshCw aria-hidden="true" className={isLoading ? "animate-spin" : ""} size={14} />
				</button>
			</div>
			{changes && changes.totalCount > 0 ? (
				<div className="flex flex-wrap gap-2">
					<ActionButton
						disabled={isBusy || workingFiles.length === 0}
						icon={<SquareCheckBig aria-hidden="true" size={14} />}
						label="Stage all"
						onClick={() => void runIndexCommand("StageWorkingTreeFiles", [], true)}
					/>
					<ActionButton
						disabled={isBusy || changes.staged.length === 0}
						icon={<MinusSquare aria-hidden="true" size={14} />}
						label="Unstage all"
						onClick={() => void runIndexCommand("UnstageWorkingTreeFiles", [], true)}
					/>
				</div>
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
					<ChangeGroup
						actionLabel={
							selectedStagedFiles.length > 0
								? `Unstage selected (${selectedStagedFiles.length})`
								: "Unstage selected"
						}
						files={changes.staged}
						isActionDisabled={isBusy || selectedStagedFiles.length === 0}
						onAction={() =>
							void runIndexCommand(
								"UnstageWorkingTreeFiles",
								selectedStagedFiles,
								false,
							)
						}
						onFileAction={(file) =>
							void runIndexCommand("UnstageWorkingTreeFiles", [file], false)
						}
						onSelectFile={onSelectFile}
						onToggleSelected={toggleSelected}
						selectedKeys={selectedKeys}
						title="Staged"
					/>
					<ChangeGroup
						actionLabel={
							selectedWorkingFiles.length > 0
								? `Stage selected (${selectedWorkingFiles.length})`
								: "Stage selected"
						}
						hideGroupLabel
						files={workingFiles}
						isActionDisabled={isBusy || selectedWorkingFiles.length === 0}
						onAction={() =>
							void runIndexCommand(
								"StageWorkingTreeFiles",
								selectedWorkingFiles,
								false,
							)
						}
						onFileAction={(file) =>
							void runIndexCommand("StageWorkingTreeFiles", [file], false)
						}
						onSelectFile={onSelectFile}
						onToggleSelected={toggleSelected}
						selectedKeys={selectedKeys}
						title="Changes"
					/>
					<ChangeGroup title="Unmerged" files={changes.unmerged} onSelectFile={onSelectFile} />
				</>
			) : null}
		</div>
	);
}
