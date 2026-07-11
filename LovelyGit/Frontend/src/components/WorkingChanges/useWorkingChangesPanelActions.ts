import { useEffect, useMemo, useState } from "react";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import {
	commitStagedChanges,
	discardWorkingChanges,
	type IndexCommandType,
	ignoreWorkingTreePath,
	loadHeadCommitMessage,
	runIndexCommand,
} from "./WorkingChangesPanelCommands";
import { fileKey } from "./WorkingChangesPanelParts";

export function useWorkingChangesPanelActions({
	changes,
	onCommitSuccess,
	onRefresh,
	repositoryId,
}: {
	changes: WorkingTreeChangesResponse | null;
	onCommitSuccess: () => Promise<void> | void;
	onRefresh: () => Promise<void> | void;
	repositoryId: string;
}) {
	const [selectedKeys, setSelectedKeys] = useState<Set<string>>(
		() => new Set(),
	);
	const [actionError, setActionError] = useState<string | null>(null);
	const [isMutating, setIsMutating] = useState(false);
	const [commitTitle, setCommitTitle] = useState("");
	const [commitBody, setCommitBody] = useState("");
	const [isCommitting, setIsCommitting] = useState(false);
	const [isAmending, setIsAmending] = useState(false);
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
				discardFiles,
				onRefresh,
				repositoryId,
				setActionError,
				setDiscardFiles,
				setIsMutating,
				setSelectedKeys,
			}),
		isBusy: isMutating || isCommitting || isLoadingAmendMessage,
		ignorePath: (path: string, target: "Local" | "Shared") =>
			ignoreWorkingTreePath({
				onRefresh,
				path,
				repositoryId,
				setActionError,
				setIsMutating,
				target,
			}),
		isAmending,
		isCommitting,
		isLoadingAmendMessage,
		isMutating,
		runIndexCommand: (
			commandType: IndexCommandType,
			files: WorkingTreeChangedFile[],
			includeAll: boolean,
		) =>
			runIndexCommand({
				commandType,
				files,
				includeAll,
				onRefresh,
				repositoryId,
				setActionError,
				setIsMutating,
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
