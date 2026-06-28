import { useEffect, useState } from "react";
import type { GitOperationState } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

type OperationStateLoad =
	| { status: "idle"; state: null }
	| { status: "loaded"; state: GitOperationState }
	| { status: "error"; state: null };

export function useGitOperationState(repositoryId: string | null) {
	const [loadState, setLoadState] = useState<OperationStateLoad>({
		status: "idle",
		state: null,
	});

	useEffect(() => {
		if (!repositoryId) {
			setLoadState({ status: "idle", state: null });
			return;
		}

		let isActive = true;
		const load = async () => {
			try {
				const state = await sendRequestWithResponse({
					arguments: { repositoryId },
					commandType: NativeMessageType.GetGitOperationState,
				});
				if (isActive) {
					setLoadState({ status: "loaded", state });
				}
			} catch {
				if (isActive) {
					setLoadState({ status: "error", state: null });
				}
			}
		};

		void load();
		const unsubscribeGraph = subscribeToServerEvent(
			"CommitGraphChanged",
			() => {
				void load();
			},
		);
		const unsubscribeWorkingTree = subscribeToServerEvent(
			"WorkingTreeChanged",
			() => {
				void load();
			},
		);
		const refreshTimer = window.setInterval(() => {
			void load();
		}, 2_000);

		return () => {
			isActive = false;
			window.clearInterval(refreshTimer);
			unsubscribeGraph();
			unsubscribeWorkingTree();
		};
	}, [repositoryId]);

	return loadState;
}
