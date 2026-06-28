import { useEffect, useRef, useState } from "react";
import { subscribeToServerEvent } from "@/lib/commands";
import {
	applyObservedWorkingTreeChanges,
	countObservedNewPaths,
} from "./OptimisticWorkingTreeChanges";
import { loadWorkingTreeChanges } from "./WorkingTreeChangesRequests";
import type { WorkingTreeChangesState } from "./WorkingTreeChangesState";

export function useWorkingTreeChanges(
	repositoryId: string | null,
	enabled: boolean,
) {
	const [state, setState] = useState<WorkingTreeChangesState>({
		status: "idle",
		changes: null,
	});
	const [isDirty, setIsDirty] = useState(false);
	const [summaryCount, setSummaryCount] = useState(0);
	const [hasSummaryLoaded, setHasSummaryLoaded] = useState(false);
	const summaryReloadTimerRef = useRef<number | null>(null);
	const previousRepositoryIdRef = useRef<string | null>(repositoryId);

	useEffect(() => {
		if (previousRepositoryIdRef.current !== repositoryId) {
			previousRepositoryIdRef.current = repositoryId;
			setState({ status: "idle", changes: null });
			setIsDirty(false);
			setSummaryCount(0);
			setHasSummaryLoaded(false);
		}

		if (!repositoryId) {
			setState({ status: "idle", changes: null });
			setIsDirty(false);
			setSummaryCount(0);
			setHasSummaryLoaded(false);
			return;
		}

		if (!enabled) {
			return;
		}

		let isActive = true;
		let isLoading = false;
		let reloadAgain = false;
		let reconcileTimer: number | null = null;
		const load = async () => {
			if (isLoading) {
				reloadAgain = true;
				return;
			}

			isLoading = true;
			setState((current) =>
				current.changes
					? { status: "loaded", changes: current.changes }
					: { status: "loading", changes: null },
			);
			try {
				const changes = await loadWorkingTreeChanges(repositoryId);
				if (isActive) {
					setState({
						status: "loaded",
						changes,
					});
					setSummaryCount(changes.totalCount);
					setIsDirty(false);
					setHasSummaryLoaded(true);
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
		const unsubscribe = subscribeToServerEvent(
			"WorkingTreeChanged",
			(event) => {
				setIsDirty(true);
				const hasObservedChanges = (event.observedChanges?.length ?? 0) > 0;
				setState((current) => {
					const newPathCount = countObservedNewPaths(
						current.changes,
						event.observedChanges,
					);
					const changes = applyObservedWorkingTreeChanges(
						current.changes,
						event.observedChanges,
					);
					if (!changes) {
						return current;
					}

					setSummaryCount((count) => count + newPathCount);
					return { status: "loaded", changes };
				});
				if (reconcileTimer != null) {
					window.clearTimeout(reconcileTimer);
				}

				reconcileTimer = hasObservedChanges
					? window.setTimeout(() => void load(), 1500)
					: null;
				if (!hasObservedChanges) {
					void load();
				}
			},
		);

		return () => {
			isActive = false;
			if (reconcileTimer != null) {
				window.clearTimeout(reconcileTimer);
			}

			unsubscribe();
		};
	}, [repositoryId, enabled]);

	useEffect(() => {
		if (!repositoryId || enabled) {
			return;
		}

		let isLoading = false;
		let reloadAgain = false;
		const loadChanges = async () => {
			if (isLoading) {
				reloadAgain = true;
				return;
			}

			isLoading = true;
			try {
				const loadedChanges = await loadWorkingTreeChanges(repositoryId);
				if (previousRepositoryIdRef.current === repositoryId) {
					setState({
						status: "loaded",
						changes: loadedChanges,
					});
					setSummaryCount(loadedChanges.totalCount);
					setIsDirty(false);
					setHasSummaryLoaded(true);
				}
			} catch {
				if (previousRepositoryIdRef.current === repositoryId) {
					setIsDirty(true);
					setHasSummaryLoaded(false);
				}
			} finally {
				isLoading = false;
				if (previousRepositoryIdRef.current === repositoryId && reloadAgain) {
					reloadAgain = false;
					scheduleChangesLoad();
				}
			}
		};

		const scheduleChangesLoad = () => {
			if (summaryReloadTimerRef.current != null) {
				window.clearTimeout(summaryReloadTimerRef.current);
			}

			summaryReloadTimerRef.current = window.setTimeout(() => {
				summaryReloadTimerRef.current = null;
				void loadChanges();
			}, 150);
		};

		void loadChanges();
		const unsubscribe = subscribeToServerEvent(
			"WorkingTreeChanged",
			(event) => {
				setIsDirty(true);
				setState((current) => {
					const changes = applyObservedWorkingTreeChanges(
						current.changes,
						event.observedChanges,
					);
					if (!changes) {
						return current;
					}

					setSummaryCount(changes.totalCount);
					return { status: "loaded", changes };
				});
				scheduleChangesLoad();
			},
		);

		return () => {
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
		isSummaryLoaded: hasSummaryLoaded,
		totalCount: state.changes?.totalCount ?? summaryCount,
		reload: async () => {
			if (!repositoryId) {
				return;
			}

			const changes = await loadWorkingTreeChanges(repositoryId);
			setState({
				status: "loaded",
				changes,
			});
			setSummaryCount(changes.totalCount);
			setIsDirty(false);
			setHasSummaryLoaded(true);
		},
	};
}
