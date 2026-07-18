import { useCallback, useEffect, useRef, useState } from "react";
import type { RemoteSyncStatusResponse } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import {
	getCachedRemoteSyncStatus,
	setCachedRemoteSyncStatus,
} from "./remoteSyncStatusCache";

export const CACHED_SYNC_REFRESH_DELAY_MS = 500;

export function useRemoteSyncStatus(
	repositoryId: string | null,
	currentBranchName: string | null,
) {
	const [status, setStatus] = useState<RemoteSyncStatusResponse | null>(null);
	const branchNameRef = useRef(currentBranchName);
	const statusRef = useRef<RemoteSyncStatusResponse | null>(null);
	const requestVersion = useRef(0);
	const refreshTimerRef = useRef<number | null>(null);
	branchNameRef.current = currentBranchName;
	const load = useCallback(async () => {
		if (refreshTimerRef.current != null) {
			window.clearTimeout(refreshTimerRef.current);
			refreshTimerRef.current = null;
		}
		const version = ++requestVersion.current;
		if (!repositoryId) {
			statusRef.current = null;
			setStatus(null);
			return;
		}

		try {
			const response = await sendRequestWithResponse({
				commandType: "GetRemoteSyncStatus",
				arguments: { repositoryId },
			});
			if (version === requestVersion.current) {
				const nextStatus =
					(response?.branchName ?? null) === branchNameRef.current
						? (response ?? null)
						: null;
				if (response) setCachedRemoteSyncStatus(repositoryId, response);
				statusRef.current = nextStatus;
				setStatus(nextStatus);
			}
		} catch (error) {
			if (version === requestVersion.current) {
				statusRef.current = null;
				setStatus(null);
				console.error(
					"Failed to read native remote synchronization status.",
					error,
				);
			}
		}
	}, [repositoryId]);

	useEffect(() => {
		requestVersion.current++;
		const cached = repositoryId
			? getCachedRemoteSyncStatus(repositoryId)
			: null;
		const visibleCached =
			(cached?.branchName ?? null) === branchNameRef.current ? cached : null;
		statusRef.current = visibleCached;
		setStatus(visibleCached);
		if (!cached) {
			void load();
		}

		return () => {
			if (refreshTimerRef.current != null) {
				window.clearTimeout(refreshTimerRef.current);
				refreshTimerRef.current = null;
			}
		};
	}, [load, repositoryId]);

	useEffect(() => {
		const loadedStatus = statusRef.current;
		if (loadedStatus?.branchName === currentBranchName) {
			return;
		}
		if (!loadedStatus && repositoryId) {
			const cached = getCachedRemoteSyncStatus(repositoryId);
			if (cached?.branchName === currentBranchName) {
				statusRef.current = cached;
				setStatus(cached);
			}
			return;
		}

		statusRef.current = null;
		setStatus(null);
		if (refreshTimerRef.current != null) {
			window.clearTimeout(refreshTimerRef.current);
		}
		refreshTimerRef.current = window.setTimeout(
			() => void load(),
			CACHED_SYNC_REFRESH_DELAY_MS,
		);
	}, [currentBranchName, load, repositoryId]);

	useEffect(
		() =>
			subscribeToServerEvent("CommitGraphChanged", () => {
				void load();
			}),
		[load],
	);

	return { reload: load, status };
}
