import { toast } from "sonner";
import type {
	GitIgnoreTarget,
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { uniquePaths } from "./WorkingChangesPanelParts";

export type IndexCommandType =
	| "StageWorkingTreeFiles"
	| "UnstageWorkingTreeFiles";

export async function ignoreWorkingTreePath({
	onRefresh,
	path,
	repositoryId,
	setActionError,
	setIsMutating,
	target,
}: {
	onRefresh: () => Promise<void> | void;
	path: string;
	repositoryId: string;
	setActionError: (message: string | null) => void;
	setIsMutating: (isMutating: boolean) => void;
	target: GitIgnoreTarget;
}) {
	setIsMutating(true);
	setActionError(null);
	try {
		const result = await sendRequestWithResponse({
			commandType: "IgnoreWorkingTreePath",
			arguments: { path, repositoryId, target },
		});
		const destination = target === "Local" ? ".git/info/exclude" : ".gitignore";
		toast.success(
			result.added
				? `Ignored ${path} in ${destination}`
				: `${path} is already listed in ${destination}`,
		);
		await onRefresh();
	} catch (error) {
		setActionError(
			error instanceof Error ? error.message : "Failed to ignore this path.",
		);
	} finally {
		setIsMutating(false);
	}
}

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
