import { useEffect, useState } from "react";
import type { WorkingTreeChangesResponse } from "@/generated/ExpressThat.LovelyGit.Services.Git.WorkingTree.Models";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/registerSignalR";

type WorkingTreeChangesState =
	| { status: "idle"; changes: null }
	| { status: "loading"; changes: WorkingTreeChangesResponse | null }
	| { status: "error"; changes: WorkingTreeChangesResponse | null; message: string }
	| { status: "loaded"; changes: WorkingTreeChangesResponse };

export function useWorkingTreeChanges(repositoryId: string | null, enabled: boolean) {
	const [state, setState] = useState<WorkingTreeChangesState>({
		status: "idle",
		changes: null,
	});
	const [isDirty, setIsDirty] = useState(false);

	useEffect(() => {
		if (!repositoryId || !enabled) {
			setState({ status: "idle", changes: null });
			setIsDirty(false);
			return;
		}

		let isActive = true;
		const load = async () => {
			setState((current) => ({
				status: current.changes ? "loading" : "loading",
				changes: current.changes,
			}));
			try {
				const changes = await sendRequestWithResponse({
					commandType: "GetWorkingTreeChanges",
					arguments: { repositoryId },
				});
				if (isActive) {
					setState({
						status: "loaded",
						changes:
							changes ?? {
								staged: [],
								unstaged: [],
								untracked: [],
								unmerged: [],
								totalCount: 0,
							},
					});
				}
			} catch (error) {
				if (isActive) {
					setState((current) => ({
						status: "error",
						changes: current.changes,
						message:
							error instanceof Error
								? error.message
								: "Failed to load working changes.",
					}));
				}
			}
		};

		void load();
		const unsubscribe = subscribeToServerEvent("WorkingTreeChanged", () => {
			setIsDirty(true);
			void load();
		});

		return () => {
			isActive = false;
			unsubscribe();
		};
	}, [repositoryId, enabled]);

	useEffect(() => {
		if (!repositoryId || enabled) {
			return;
		}

		const unsubscribe = subscribeToServerEvent("WorkingTreeChanged", () => {
			setIsDirty(true);
		});

		return unsubscribe;
	}, [repositoryId, enabled]);

	return {
		...state,
		isDirty,
		reload: async () => {
			if (!repositoryId) {
				return;
			}

			const changes = await sendRequestWithResponse({
				commandType: "GetWorkingTreeChanges",
				arguments: { repositoryId },
			});
			setState({
				status: "loaded",
				changes:
					changes ?? {
						staged: [],
						unstaged: [],
						untracked: [],
						unmerged: [],
						totalCount: 0,
					},
			});
			setIsDirty(false);
		},
	};
}
