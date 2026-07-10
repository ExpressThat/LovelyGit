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
	const requestVersion = useRef(0);
	const load = useCallback(async () => {
		const version = ++requestVersion.current;
		if (!repositoryId) {
			setStatus(null);
			return;
		}

		try {
			const response = await sendRequestWithResponse({
				commandType: "GetRemoteSyncStatus",
				arguments: { repositoryId },
			});
			if (version === requestVersion.current) {
				setStatus(
					(response?.branchName ?? null) === currentBranchName
						? (response ?? null)
						: null,
				);
			}
		} catch (error) {
			if (version === requestVersion.current) setStatus(null);
			console.error(
				"Failed to read native remote synchronization status.",
				error,
			);
		}
	}, [currentBranchName, repositoryId]);

	useEffect(() => {
		setStatus(null);
		void load();
	}, [load]);

	useEffect(
		() =>
			subscribeToServerEvent("CommitGraphChanged", () => {
				void load();
			}),
		[load],
	);

	return { reload: load, status };
}
