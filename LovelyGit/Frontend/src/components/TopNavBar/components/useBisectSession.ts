import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import type { GitBisectAction, GitBisectState } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import { getCachedBisectState, setCachedBisectState } from "./bisectStateCache";

export const CACHED_BISECT_REFRESH_DELAY_MS = 500;

export function useBisectSession(repositoryId: string | null) {
	const [state, setState] = useState<GitBisectState | null>(null);
	const [isLoading, setIsLoading] = useState(false);
	const [busyAction, setBusyAction] = useState<GitBisectAction | null>(null);
	const requestId = useRef(0);
	const refreshTimerRef = useRef<number | null>(null);

	const load = useCallback(async () => {
		if (refreshTimerRef.current != null) {
			window.clearTimeout(refreshTimerRef.current);
			refreshTimerRef.current = null;
		}
		if (!repositoryId) {
			setState(null);
			return;
		}
		const currentRequest = ++requestId.current;
		setIsLoading(true);
		try {
			const response = await sendRequestWithResponse({
				commandType: "GetBisectState",
				arguments: { repositoryId },
			});
			if (currentRequest === requestId.current) {
				setCachedBisectState(repositoryId, response);
				setState(response);
			}
		} catch (error) {
			if (currentRequest === requestId.current) {
				toast.error(message(error, "Could not read the bisect session"));
			}
		} finally {
			if (currentRequest === requestId.current) setIsLoading(false);
		}
	}, [repositoryId]);

	useEffect(() => {
		requestId.current++;
		const cached = repositoryId ? getCachedBisectState(repositoryId) : null;
		setState(cached);
		setIsLoading(false);
		if (repositoryId && !cached) {
			refreshTimerRef.current = window.setTimeout(
				() => void load(),
				CACHED_BISECT_REFRESH_DELAY_MS,
			);
		}
		const unsubscribe = subscribeToServerEvent(
			"CommitGraphChanged",
			() => void load(),
		);
		return () => {
			if (refreshTimerRef.current != null) {
				window.clearTimeout(refreshTimerRef.current);
				refreshTimerRef.current = null;
			}
			unsubscribe();
		};
	}, [load, repositoryId]);

	async function run(action: GitBisectAction) {
		if (!repositoryId || busyAction) return;
		setBusyAction(action);
		const toastId = toast.loading(`${actionLabel(action)}…`);
		try {
			const response = await sendRequestWithResponse(
				{
					commandType: "ManageBisect",
					arguments: { action, goodCommit: null, repositoryId },
				},
				{ timeoutMs: 30_000 },
			);
			setCachedBisectState(repositoryId, response);
			setState(response);
			toast.success(successLabel(action, response), { id: toastId });
		} catch (error) {
			toast.error(message(error, "Git could not update the bisect session"), {
				id: toastId,
			});
		} finally {
			setBusyAction(null);
		}
	}

	return { busyAction, isLoading, load, run, state };
}

function actionLabel(action: GitBisectAction) {
	if (action === "MarkGood") return "Marking this revision good";
	if (action === "MarkBad") return "Marking this revision bad";
	if (action === "Skip") return "Skipping this revision";
	return "Resetting bisect";
}

function successLabel(action: GitBisectAction, state: GitBisectState) {
	if (action === "Reset") return "Bisect session reset";
	if (state.firstBadCommit) return "First bad commit found";
	return `${actionLabel(action)} complete`;
}

function message(error: unknown, fallback: string) {
	return error instanceof Error && error.message ? error.message : fallback;
}
