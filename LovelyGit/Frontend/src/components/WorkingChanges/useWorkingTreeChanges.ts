import { useEffect, useRef, useState } from "react";
import { subscribeToServerEvent } from "@/lib/commands";
import {
	applyObservedWorkingTreeChanges,
	countObservedNewPaths,
	shouldApplyObservedWorkingTreeChanges,
} from "./OptimisticWorkingTreeChanges";
import { useWorkingTreePreload } from "./useWorkingTreePreload";
import { loadWorkingTreeChanges } from "./WorkingTreeChangesRequests";
import type { WorkingTreeChangesState } from "./WorkingTreeChangesState";
import {
	cacheCompleteWorkingTreeSummary,
	invalidateWorkingTreeSummary,
} from "./workingTreeSummaryCache";

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
	const hasFreshChangesRef = useRef(false);
	const previousRepositoryIdRef = useRef<string | null>(repositoryId);

	useEffect(() => {
		if (previousRepositoryIdRef.current !== repositoryId) {
			previousRepositoryIdRef.current = repositoryId;
			setState({ status: "idle", changes: null });
			setIsDirty(false);
			setSummaryCount(0);
			setHasSummaryLoaded(false);
			hasFreshChangesRef.current = false;
		}

		if (!repositoryId) {
			setState({ status: "idle", changes: null });
			setIsDirty(false);
			setSummaryCount(0);
			setHasSummaryLoaded(false);
			hasFreshChangesRef.current = false;
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
					cacheCompleteWorkingTreeSummary(repositoryId, changes.totalCount);
					setIsDirty(false);
					setHasSummaryLoaded(true);
					hasFreshChangesRef.current = true;
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

		if (!hasFreshChangesRef.current) void load();
		const unsubscribe = subscribeToServerEvent(
			"WorkingTreeChanged",
			(event) => {
				invalidateWorkingTreeSummary(repositoryId);
				hasFreshChangesRef.current = false;
				setIsDirty(true);
				const applyObserved = shouldApplyObservedWorkingTreeChanges(
					event.observedChanges,
				);
				if (applyObserved) {
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
				}
				if (reconcileTimer != null) {
					window.clearTimeout(reconcileTimer);
				}

				reconcileTimer = applyObserved
					? window.setTimeout(() => void load(), 500)
					: window.setTimeout(() => void load(), 0);
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

	useWorkingTreePreload({
		enabled,
		hasFreshChangesRef,
		previousRepositoryIdRef,
		repositoryId,
		setHasSummaryLoaded,
		setIsDirty,
		setState,
		setSummaryCount,
	});

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
			cacheCompleteWorkingTreeSummary(repositoryId, changes.totalCount);
			setIsDirty(false);
			setHasSummaryLoaded(true);
			hasFreshChangesRef.current = true;
		},
	};
}
