import { useEffect, useMemo, useState } from "react";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { useSetting } from "@/lib/settings/settingsStore";
import { discardWorkingChanges } from "./WorkingChangesDiscardCommand";
import { ignoreWorkingTreePath } from "./WorkingChangesIgnoreCommand";
import {
	commitStagedChanges,
	type IndexCommandType,
	loadHeadCommitMessage,
	runIndexCommand,
} from "./WorkingChangesPanelCommands";
import { fileKey } from "./WorkingChangesPanelParts";

export function useWorkingChangesPanelActions({
	clearOptimisticChanges,
	changes,
	isOptimisticChangesCurrent,
	onCommitSuccess,
	onRefresh,
	repositoryId,
	setOptimisticChanges,
}: {
	clearOptimisticChanges: (expected: WorkingTreeChangesResponse) => void;
	changes: WorkingTreeChangesResponse | null;
	isOptimisticChangesCurrent: (expected: WorkingTreeChangesResponse) => boolean;
	onCommitSuccess: () => Promise<void> | void;
	onRefresh: () => Promise<void> | void;
	repositoryId: string;
	setOptimisticChanges: (changes: WorkingTreeChangesResponse | null) => void;
}) {
	const signCommitsByDefault = useSetting("SignCommitsByDefault");
	const [selectedKeys, setSelectedKeys] = useState<Set<string>>(
		() => new Set(),
	);
	const [actionError, setActionError] = useState<string | null>(null);
	const [isMutating, setIsMutating] = useState(false);
	const [commitTitle, setCommitTitle] = useState("");
	const [commitBody, setCommitBody] = useState("");
	const [isCommitting, setIsCommitting] = useState(false);
	const [isAmending, setIsAmending] = useState(false);
	const [isSigningCommit, setIsSigningCommit] = useState(
		() => signCommitsByDefault,
	);
	const [isLoadingAmendMessage, setIsLoadingAmendMessage] = useState(false);
	const [draftBeforeAmend, setDraftBeforeAmend] = useState({
		body: "",
		title: "",
	});
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

	useEffect(() => {
		setSelectedKeys((current) => {
			const next = new Set([...current].filter((key) => fileKeys.has(key)));
			return next.size === current.size ? current : next;
		});
	}, [fileKeys]);

	return {
		actionError,
		commitBody,
		commitStagedChanges: () =>
			commitStagedChanges({
				amend: isAmending,
				changes,
				commitBody,
				commitTitle,
				onCommitSuccess,
				repositoryId,
				sign: isSigningCommit,
				setActionError,
				setCommitBody,
				setCommitTitle,
				setIsAmending,
				setIsCommitting,
				setSelectedKeys,
			}),
		commitTitle,
		discardFiles,
		discardWorkingChanges: () =>
			discardWorkingChanges({
				changes,
				discardFiles,
				onRefresh,
				repositoryId,
				setActionError,
				setDiscardFiles,
				setIsMutating,
				setOptimisticChanges,
				setSelectedKeys,
			}),
		isBusy: isMutating || isCommitting || isLoadingAmendMessage,
		ignorePath: (path: string, target: "Local" | "Shared") =>
			ignoreWorkingTreePath({
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
			}),
		isAmending,
		isCommitting,
		isLoadingAmendMessage,
		isSigningCommit,
		isMutating,
		runIndexCommand: (
			commandType: IndexCommandType,
			files: WorkingTreeChangedFile[],
			includeAll: boolean,
		) =>
			runIndexCommand({
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
			}),
		selectedKeys,
		restoreCommitDraft: (title: string, body: string) => {
			setCommitTitle(title);
			setCommitBody(body);
			setIsAmending(false);
			setDraftBeforeAmend({ body: "", title: "" });
		},
		setCommitBody,
		setCommitTitle,
		setDiscardFiles,
		setIsSigningCommit,
		toggleSelected: (file: WorkingTreeChangedFile) => {
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
		},
		toggleAmend: async (enabled: boolean) => {
			if (!enabled) {
				setIsAmending(false);
				setCommitTitle(draftBeforeAmend.title);
				setCommitBody(draftBeforeAmend.body);
				return;
			}

			setIsLoadingAmendMessage(true);
			setActionError(null);
			try {
				const message = await loadHeadCommitMessage(repositoryId);
				setDraftBeforeAmend({ body: commitBody, title: commitTitle });
				setCommitTitle(message.title);
				setCommitBody(message.body);
				setIsAmending(true);
			} catch (error) {
				setActionError(
					error instanceof Error
						? error.message
						: "Failed to load the last commit message.",
				);
			} finally {
				setIsLoadingAmendMessage(false);
			}
		},
	};
}
