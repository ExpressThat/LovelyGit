import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { uniquePaths } from "./WorkingChangesPanelParts";

export type IndexCommandType =
	| "StageWorkingTreeFiles"
	| "UnstageWorkingTreeFiles";

export async function runIndexCommand({
	commandType,
	files,
	includeAll,
	onRefresh,
	repositoryId,
	setActionError,
	setIsMutating,
	setSelectedKeys,
}: {
	commandType: IndexCommandType;
	files: WorkingTreeChangedFile[];
	includeAll: boolean;
	onRefresh: () => Promise<void> | void;
	repositoryId: string;
	setActionError: (message: string | null) => void;
	setIsMutating: (isMutating: boolean) => void;
	setSelectedKeys: (keys: Set<string>) => void;
}) {
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
	} catch (error) {
		setActionError(
			error instanceof Error ? error.message : "Failed to update the index.",
		);
	} finally {
		setIsMutating(false);
	}
}

export async function commitStagedChanges({
	changes,
	commitBody,
	commitTitle,
	onCommitSuccess,
	repositoryId,
	setActionError,
	setCommitBody,
	setCommitTitle,
	setIsCommitting,
	setSelectedKeys,
}: {
	changes: WorkingTreeChangesResponse | null;
	commitBody: string;
	commitTitle: string;
	onCommitSuccess: () => Promise<void> | void;
	repositoryId: string;
	setActionError: (message: string | null) => void;
	setCommitBody: (body: string) => void;
	setCommitTitle: (title: string) => void;
	setIsCommitting: (isCommitting: boolean) => void;
	setSelectedKeys: (keys: Set<string>) => void;
}) {
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
	} catch (error) {
		setActionError(
			error instanceof Error
				? error.message
				: "Failed to commit staged changes.",
		);
	} finally {
		setIsCommitting(false);
	}
}

export async function discardWorkingChanges({
	discardFiles,
	onRefresh,
	repositoryId,
	setActionError,
	setDiscardFiles,
	setIsMutating,
	setSelectedKeys,
}: {
	discardFiles: WorkingTreeChangedFile[];
	onRefresh: () => Promise<void> | void;
	repositoryId: string;
	setActionError: (message: string | null) => void;
	setDiscardFiles: (files: WorkingTreeChangedFile[]) => void;
	setIsMutating: (isMutating: boolean) => void;
	setSelectedKeys: (keys: Set<string>) => void;
}) {
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
	} catch (error) {
		setActionError(
			error instanceof Error
				? error.message
				: "Failed to discard selected changes.",
		);
	} finally {
		setIsMutating(false);
	}
}
