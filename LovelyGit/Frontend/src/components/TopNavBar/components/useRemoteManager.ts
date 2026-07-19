import { useEffect, useState } from "react";
import { toast } from "sonner";
import type { GitRemote } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { expandRemotePayload } from "@/lib/remotePayload";

export type RemoteDraft = {
	name: string;
	originalName: string | null;
	pushUrl: string;
	url: string;
};

export function useRemoteManager(repositoryId: string | null, open: boolean) {
	const [remotes, setRemotes] = useState<GitRemote[]>([]);
	const [editor, setEditor] = useState<RemoteDraft | null>(null);
	const [removeTarget, setRemoveTarget] = useState<GitRemote | null>(null);
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(false);
	const [isMutating, setIsMutating] = useState(false);

	useEffect(() => {
		if (!open || !repositoryId) return;
		let active = true;
		setIsLoading(true);
		setError(null);
		void loadRemotes(repositoryId)
			.then((result) => {
				if (active) setRemotes(result);
			})
			.catch((reason) => {
				if (active) setError(errorMessage(reason, "Failed to load remotes."));
			})
			.finally(() => {
				if (active) setIsLoading(false);
			});
		return () => {
			active = false;
		};
	}, [open, repositoryId]);

	return {
		editor,
		error,
		isLoading,
		isMutating,
		remotes,
		removeTarget,
		cancelEdit: () => setEditor(null),
		closeRemove: () => !isMutating && setRemoveTarget(null),
		confirmRemove: async () => {
			if (!repositoryId || !removeTarget || isMutating) return;
			setIsMutating(true);
			setError(null);
			try {
				await mutateRemote(repositoryId, {
					action: "Remove",
					name: removeTarget.name,
				});
				toast.success(`Removed ${removeTarget.name}`);
				setRemotes((current) =>
					current.filter((remote) => remote.name !== removeTarget.name),
				);
				setRemoveTarget(null);
			} catch (reason) {
				setError(errorMessage(reason, "Failed to remove the remote."));
			} finally {
				setIsMutating(false);
			}
		},
		save: async (draft: RemoteDraft) => {
			if (!repositoryId || isMutating) return;
			setIsMutating(true);
			setError(null);
			try {
				await mutateRemote(repositoryId, {
					action: draft.originalName ? "Update" : "Add",
					name: draft.originalName ?? draft.name,
					newName: draft.name,
					pushUrl: draft.pushUrl || null,
					url: draft.url,
				});
				toast.success(
					draft.originalName ? `Updated ${draft.name}` : `Added ${draft.name}`,
				);
				setRemotes((current) => upsertRemote(current, draft));
				setEditor(null);
			} catch (reason) {
				setError(errorMessage(reason, "Failed to save the remote."));
			} finally {
				setIsMutating(false);
			}
		},
		startAdd: () =>
			setEditor({ name: "", originalName: null, pushUrl: "", url: "" }),
		startEdit: (remote: GitRemote) =>
			setEditor({
				name: remote.name,
				originalName: remote.name,
				pushUrl: remote.pushUrl ?? "",
				url: remote.url,
			}),
		startRemove: setRemoveTarget,
	};
}

function loadRemotes(repositoryId: string) {
	return sendRequestWithResponse({
		arguments: { repositoryId },
		commandType: "GetRemotes",
	}).then(expandRemotePayload);
}

function mutateRemote(
	repositoryId: string,
	arguments_: {
		action: "Add" | "Remove" | "Update";
		name: string;
		newName?: string;
		pushUrl?: string | null;
		url?: string;
	},
) {
	return sendRequestWithResponse(
		{
			arguments: {
				action: arguments_.action,
				name: arguments_.name,
				newName: arguments_.newName ?? null,
				pushUrl: arguments_.pushUrl ?? null,
				repositoryId,
				url: arguments_.url ?? null,
			},
			commandType: "ManageRemote",
		},
		{ timeoutMs: gitMutationTimeoutMs },
	);
}

function errorMessage(reason: unknown, fallback: string) {
	return reason instanceof Error ? reason.message : fallback;
}

function upsertRemote(remotes: GitRemote[], draft: RemoteDraft) {
	const remote: GitRemote = {
		name: draft.name.trim(),
		pushUrl: draft.pushUrl.trim() || null,
		url: draft.url.trim(),
	};
	const next = draft.originalName
		? remotes.map((candidate) =>
				candidate.name === draft.originalName ? remote : candidate,
			)
		: [...remotes, remote];
	return next.sort((left, right) => left.name.localeCompare(right.name));
}
