import { useEffect, useState } from "react";
import type { WorkingTreeChangesResponse } from "@/generated/ExpressThat.LovelyGit.Services.Git.WorkingTree.Models";
import {
	sendRequestWithResponse,
	subscribeToWorkingTreeChanged,
} from "@/lib/registerSignalR";

type WorkingTreeChangesState =
	| { status: "idle"; changes: null }
	| { status: "loading"; changes: WorkingTreeChangesResponse | null }
	| { status: "error"; changes: WorkingTreeChangesResponse | null; message: string }
	| { status: "loaded"; changes: WorkingTreeChangesResponse };

export function useWorkingTreeChanges(repositoryId: string | null) {
	const [state, setState] = useState<WorkingTreeChangesState>({
		status: "idle",
		changes: null,
	});

	useEffect(() => {
		if (!repositoryId) {
			setState({ status: "idle", changes: null });
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
		const unsubscribe = subscribeToWorkingTreeChanged(() => {
			void load();
		});

		return () => {
			isActive = false;
			unsubscribe();
		};
	}, [repositoryId]);

	return {
		...state,
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
		},
	};
}
