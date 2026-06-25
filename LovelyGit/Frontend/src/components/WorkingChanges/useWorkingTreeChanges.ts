import { useEffect, useRef, useState } from "react";
import type { WorkingTreeChangesResponse } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";

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
	const [summaryCount, setSummaryCount] = useState(0);
	const summaryReloadTimerRef = useRef<number | null>(null);

	useEffect(() => {
		if (!repositoryId) {
			setState({ status: "idle", changes: null });
			setIsDirty(false);
			setSummaryCount(0);
			return;
		}

		if (!enabled) {
			setState({ status: "idle", changes: null });
			return;
		}

		let isActive = true;
		let isLoading = false;
		let reloadAgain = false;
		const load = async () => {
			if (isLoading) {
				reloadAgain = true;
				return;
			}

			isLoading = true;
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
					setSummaryCount(changes?.totalCount ?? 0);
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
			} finally {
				isLoading = false;
				if (isActive && reloadAgain) {
					reloadAgain = false;
					void load();
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

		let isActive = true;
		let isLoading = false;
		let reloadAgain = false;
		const loadSummary = async () => {
			if (isLoading) {
				reloadAgain = true;
				return;
			}

			isLoading = true;
			try {
				const summary = await sendRequestWithResponse({
					commandType: "GetWorkingTreeChangeSummary",
					arguments: { repositoryId },
				});
				if (isActive) {
					setSummaryCount(summary?.totalCount ?? 0);
					setIsDirty(false);
				}
			} catch {
				if (isActive) {
					setIsDirty(true);
				}
			} finally {
				isLoading = false;
				if (isActive && reloadAgain) {
					reloadAgain = false;
					scheduleSummaryLoad();
				}
			}
		};

		const scheduleSummaryLoad = () => {
			if (summaryReloadTimerRef.current != null) {
				window.clearTimeout(summaryReloadTimerRef.current);
			}

			summaryReloadTimerRef.current = window.setTimeout(() => {
				summaryReloadTimerRef.current = null;
				void loadSummary();
			}, 150);
		};

		void loadSummary();
		const unsubscribe = subscribeToServerEvent("WorkingTreeChanged", () => {
			setIsDirty(true);
			scheduleSummaryLoad();
		});

		return () => {
			isActive = false;
			if (summaryReloadTimerRef.current != null) {
				window.clearTimeout(summaryReloadTimerRef.current);
				summaryReloadTimerRef.current = null;
			}
			unsubscribe();
		};
	}, [repositoryId, enabled]);

	return {
		...state,
		isDirty,
		totalCount: state.changes?.totalCount ?? summaryCount,
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
			setSummaryCount(changes?.totalCount ?? 0);
			setIsDirty(false);
		},
	};
}
