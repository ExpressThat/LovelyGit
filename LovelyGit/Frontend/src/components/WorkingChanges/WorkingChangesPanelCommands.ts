import { flushSync } from "react-dom";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { applyOptimisticIndexMutation } from "./OptimisticWorkingTreeIndex";
import { uniquePaths } from "./WorkingChangesPanelParts";
import { waitForWorkingTreePaint } from "./WorkingTreePaintBoundary";

export type IndexCommandType =
	| "StageWorkingTreeFiles"
	| "UnstageWorkingTreeFiles";

export async function runIndexCommand({
	changes,
	commandType,
	files,
	includeAll,
	onRefresh,
	repositoryId,
	setActionError,
	setIsMutating,
	setOptimisticChanges,
	setSelectedKeys,
}: {
	changes: WorkingTreeChangesResponse | null;
	commandType: IndexCommandType;
	files: WorkingTreeChangedFile[];
	includeAll: boolean;
	onRefresh: () => Promise<void> | void;
	repositoryId: string;
	setActionError: (message: string | null) => void;
	setIsMutating: (isMutating: boolean) => void;
	setOptimisticChanges: (changes: WorkingTreeChangesResponse | null) => void;
	setSelectedKeys: (keys: Set<string>) => void;
}) {
	if (!changes || (!includeAll && files.length === 0)) {
		return;
	}

	flushSync(() => {
		setIsMutating(true);
		setActionError(null);
		setOptimisticChanges(
			applyOptimisticIndexMutation(
				changes,
				commandType === "StageWorkingTreeFiles" ? "stage" : "unstage",
				files,
				includeAll,
			),
		);
	});
	await waitForWorkingTreePaint();
	let indexUpdated = false;
	try {
		await sendRequestWithResponse({
			commandType,
			arguments: {
				includeAll,
				paths: includeAll ? [] : uniquePaths(files),
				repositoryId,
			},
		});
		indexUpdated = true;
		setSelectedKeys(new Set());
		await onRefresh();
		setOptimisticChanges(null);
	} catch (error) {
		if (!indexUpdated) {
			setOptimisticChanges(null);
		}
		setActionError(
			error instanceof Error
				? error.message
				: indexUpdated
					? "The index was updated, but its status could not be refreshed."
					: "Failed to update the index.",
		);
	} finally {
		setIsMutating(false);
	}
}

export async function commitStagedChanges({
	amend,
	changes,
	commitBody,
	commitTitle,
	onCommitSuccess,
	repositoryId,
	sign,
	setActionError,
	setCommitBody,
	setCommitTitle,
	setIsAmending,
	setIsCommitting,
	setSelectedKeys,
}: {
	amend: boolean;
	changes: WorkingTreeChangesResponse | null;
	commitBody: string;
	commitTitle: string;
	onCommitSuccess: () => Promise<void> | void;
	repositoryId: string;
	sign: boolean;
	setActionError: (message: string | null) => void;
	setCommitBody: (body: string) => void;
	setCommitTitle: (title: string) => void;
	setIsAmending: (isAmending: boolean) => void;
	setIsCommitting: (isCommitting: boolean) => void;
	setSelectedKeys: (keys: Set<string>) => void;
}) {
	if (
		!changes ||
		(!amend && changes.staged.length === 0) ||
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
				amend,
				body: commitBody,
				repositoryId,
				sign,
				title: commitTitle,
			},
		});
		setCommitTitle("");
		setCommitBody("");
		setIsAmending(false);
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

export async function loadHeadCommitMessage(repositoryId: string) {
	return sendRequestWithResponse({
		commandType: "GetHeadCommitMessage",
		arguments: { repositoryId },
	});
}
