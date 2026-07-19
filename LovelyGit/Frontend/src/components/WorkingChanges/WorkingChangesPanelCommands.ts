import { flushSync } from "react-dom";
import { toast } from "sonner";
import type {
	GitIgnoreTarget,
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { applyOptimisticIgnore } from "./OptimisticWorkingTreeIgnore";
import { applyOptimisticIndexMutation } from "./OptimisticWorkingTreeIndex";
import { uniquePaths } from "./WorkingChangesPanelParts";
import { waitForWorkingTreePaint } from "./WorkingTreePaintBoundary";

export type IndexCommandType =
	| "StageWorkingTreeFiles"
	| "UnstageWorkingTreeFiles";

export async function ignoreWorkingTreePath({
	clearOptimisticChanges,
	changes,
	isOptimisticChangesCurrent,
	onRefresh,
	path,
	repositoryId,
	setActionError,
	setIsMutating,
	setOptimisticChanges,
	target,
}: {
	clearOptimisticChanges: (expected: WorkingTreeChangesResponse) => void;
	changes: WorkingTreeChangesResponse | null;
	isOptimisticChangesCurrent: (expected: WorkingTreeChangesResponse) => boolean;
	onRefresh: () => Promise<void> | void;
	path: string;
	repositoryId: string;
	setActionError: (message: string | null) => void;
	setIsMutating: (isMutating: boolean) => void;
	setOptimisticChanges: (changes: WorkingTreeChangesResponse | null) => void;
	target: GitIgnoreTarget;
}) {
	const optimisticChanges = changes
		? applyOptimisticIgnore(changes, path)
		: null;
	flushSync(() => {
		setIsMutating(true);
		setActionError(null);
		if (optimisticChanges) setOptimisticChanges(optimisticChanges);
	});
	await waitForWorkingTreePaint();
	let ignoreUpdated = false;
	try {
		const result = await sendRequestWithResponse({
			commandType: "IgnoreWorkingTreePath",
			arguments: { path, repositoryId, target },
		});
		ignoreUpdated = true;
		const destination = target === "Local" ? ".git/info/exclude" : ".gitignore";
		toast.success(
			result.added
				? `Ignored ${path} in ${destination}`
				: `${path} is already listed in ${destination}`,
		);
		if (target === "Local" && optimisticChanges) {
			void reconcileLocalIgnore(
				onRefresh,
				() => isOptimisticChangesCurrent(optimisticChanges),
				() => clearOptimisticChanges(optimisticChanges),
				setActionError,
			);
			return;
		}
		await onRefresh();
		if (optimisticChanges) clearOptimisticChanges(optimisticChanges);
	} catch (error) {
		if (!ignoreUpdated) setOptimisticChanges(null);
		setActionError(
			error instanceof Error
				? error.message
				: ignoreUpdated
					? "The path was ignored, but its status could not be refreshed."
					: "Failed to ignore this path.",
		);
	} finally {
		setIsMutating(false);
	}
}

async function reconcileLocalIgnore(
	onRefresh: () => Promise<void> | void,
	isCurrent: () => boolean,
	onSuccess: () => void,
	setActionError: (message: string | null) => void,
) {
	try {
		await onRefresh();
		if (isCurrent()) onSuccess();
	} catch {
		if (isCurrent()) {
			setActionError(
				"The path was ignored, but its status could not be refreshed.",
			);
		}
	}
}

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
