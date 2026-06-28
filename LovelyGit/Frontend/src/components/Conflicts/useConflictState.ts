import { useCallback, useEffect, useState } from "react";
import type { GitConflictStateResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

type ConflictStateLoad =
	| { status: "idle"; state: null; message?: string }
	| { status: "loading"; state: GitConflictStateResponse | null }
	| { status: "loaded"; state: GitConflictStateResponse }
	| { status: "error"; state: null; message: string };

export function useConflictState(repositoryId: string | null) {
	const [loadState, setLoadState] = useState<ConflictStateLoad>({
		status: "idle",
		state: null,
	});

	const reload = useCallback(async () => {
		if (!repositoryId) {
			setLoadState({ status: "idle", state: null });
			return null;
		}

		setLoadState((current) => ({
			status: "loading",
			state: current.state,
		}));
		try {
			const state = await sendRequestWithResponse({
				arguments: { repositoryId },
				commandType: NativeMessageType.GetConflictState,
			});
			setLoadState({ status: "loaded", state });
			return state;
		} catch (error) {
			const message =
				error instanceof Error ? error.message : "Could not load conflicts.";
			setLoadState({ status: "error", state: null, message });
			return null;
		}
	}, [repositoryId]);

	useEffect(() => {
		void reload();
	}, [reload]);

	return { ...loadState, reload };
}
