import { useCallback, useEffect, useRef, useState } from "react";
import type { GitConflictStateResponse } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { subscribeGitOperationChanged } from "./ConflictOperationEvents";

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
	const loadSequenceRef = useRef(0);

	const reload = useCallback(async () => {
		const loadSequence = ++loadSequenceRef.current;
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
			if (loadSequenceRef.current === loadSequence) {
				setLoadState({ status: "loaded", state });
			}
			return state;
		} catch (error) {
			const message =
				error instanceof Error ? error.message : "Could not load conflicts.";
			if (loadSequenceRef.current === loadSequence) {
				setLoadState({ status: "error", state: null, message });
			}
			return null;
		}
	}, [repositoryId]);

	useEffect(() => {
		void reload();
	}, [reload]);

	useEffect(() => {
		if (!repositoryId) {
			return;
		}

		const unsubscribeOperation = subscribeGitOperationChanged((detail) => {
			if (detail.repositoryId !== repositoryId) {
				return;
			}

			if (detail.state) {
				setLoadState({ status: "loaded", state: detail.state });
				return;
			}

			void reload();
		});
		const unsubscribeWorkingTree = subscribeToServerEvent(
			"WorkingTreeChanged",
			() => {
				void reload();
			},
		);
		const unsubscribeGraph = subscribeToServerEvent(
			"CommitGraphChanged",
			() => {
				void reload();
			},
		);
		const refreshTimer = window.setInterval(() => {
			void reload();
		}, 2_000);

		return () => {
			window.clearInterval(refreshTimer);
			unsubscribeOperation();
			unsubscribeWorkingTree();
			unsubscribeGraph();
		};
	}, [reload, repositoryId]);

	return { ...loadState, reload };
}
