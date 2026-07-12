import { useCallback, useEffect, useRef, useState } from "react";
import type { RemoteSyncStatusResponse } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";

export function useRemoteSyncStatus(
	repositoryId: string | null,
	currentBranchName: string | null,
) {
	const [status, setStatus] = useState<RemoteSyncStatusResponse | null>(null);
	const branchNameRef = useRef(currentBranchName);
	const statusRef = useRef<RemoteSyncStatusResponse | null>(null);
	const requestVersion = useRef(0);
	branchNameRef.current = currentBranchName;
	const load = useCallback(async () => {
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
				statusRef.current = nextStatus;
				setStatus(nextStatus);
			}
		} catch (error) {
			if (version === requestVersion.current) {
				statusRef.current = null;
				setStatus(null);
			}
			console.error(
				"Failed to read native remote synchronization status.",
				error,
			);
		}
	}, [repositoryId]);

	useEffect(() => {
		statusRef.current = null;
		setStatus(null);
		void load();
	}, [load]);

	useEffect(() => {
		const loadedStatus = statusRef.current;
		if (!loadedStatus || loadedStatus.branchName === currentBranchName) {
			return;
		}

		statusRef.current = null;
		setStatus(null);
		void load();
	}, [currentBranchName, load]);

	useEffect(
		() =>
			subscribeToServerEvent("CommitGraphChanged", () => {
				void load();
			}),
		[load],
	);

	return { reload: load, status };
}
