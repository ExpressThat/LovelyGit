import { useState } from "react";
import { toast } from "sonner";
import type { GitLfsAction, LfsRepositoryState } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function useLfsManager(repositoryId: string | null) {
	const [state, setState] = useState<LfsRepositoryState | null>(null);
	const [isLoading, setIsLoading] = useState(false);
	const [busyAction, setBusyAction] = useState<GitLfsAction | null>(null);
	const [busyPattern, setBusyPattern] = useState<string | null>(null);
	const [error, setError] = useState<string | null>(null);

	async function load() {
		if (!repositoryId || isLoading) return;
		setIsLoading(true);
		setError(null);
		try {
			setState(
				await sendRequestWithResponse({
					commandType: NativeMessageType.GetGitLfsState,
					arguments: { repositoryId },
				}),
			);
		} catch (loadError) {
			setError(message(loadError, "Could not read Git LFS state"));
		} finally {
			setIsLoading(false);
		}
	}

	async function run(action: GitLfsAction, pattern: string | null = null) {
		if (!repositoryId || busyAction) return false;
		setBusyAction(action);
		setBusyPattern(pattern);
		try {
			const nextState = await sendRequestWithResponse(
				{
					commandType: NativeMessageType.ManageGitLfs,
					arguments: { action, pattern, repositoryId },
				},
				{ timeoutMs: 120_000 },
			);
			setState(nextState);
			setError(null);
			toast.success(successMessage(action));
			return true;
		} catch (runError) {
			toast.error(
				message(runError, "Git LFS could not complete the operation"),
				{
					duration: 8_000,
				},
			);
			return false;
		} finally {
			setBusyAction(null);
			setBusyPattern(null);
		}
	}

	return { busyAction, busyPattern, error, isLoading, load, run, state };
}

function successMessage(action: GitLfsAction) {
	if (action === "Install") return "Git LFS initialized for this repository";
	if (action === "Track") return "LFS pattern tracked";
	if (action === "Untrack") return "LFS pattern removed";
	if (action === "Fetch") return "LFS objects fetched";
	if (action === "Pull") return "LFS objects updated";
	return "Unused LFS objects pruned";
}

function message(error: unknown, fallback: string) {
	return error instanceof Error && error.message ? error.message : fallback;
}
