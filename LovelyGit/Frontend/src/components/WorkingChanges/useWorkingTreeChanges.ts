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
	getCachedWorkingTreeChanges,
	invalidateCachedWorkingTreeChanges,
	setCachedWorkingTreeChanges,
} from "./workingTreeChangesCache";
import {
	cacheCompleteWorkingTreeSummary,
	invalidateWorkingTreeSummary,
} from "./workingTreeSummaryCache";

export function useWorkingTreeChanges(
	repositoryId: string | null,
	enabled: boolean,
) {
	const [initialChanges] = useState(() =>
		repositoryId ? getCachedWorkingTreeChanges(repositoryId) : null,
	);
	const [state, setState] = useState<WorkingTreeChangesState>(() =>
		initialChanges
			? { status: "loaded", changes: initialChanges }
			: { status: "idle", changes: null },
	);
	const [isDirty, setIsDirty] = useState(false);
	const [summaryCount, setSummaryCount] = useState(
		initialChanges?.totalCount ?? 0,
	);
	const [hasSummaryLoaded, setHasSummaryLoaded] = useState(
		initialChanges !== null,
	);
	const [isReloading, setIsReloading] = useState(false);
	const hasFreshChangesRef = useRef(initialChanges !== null);
	const reloadRequestRef = useRef<ReloadRequest | null>(null);
	const previousRepositoryIdRef = useRef<string | null>(repositoryId);

	useEffect(() => {
		if (previousRepositoryIdRef.current !== repositoryId) {
			const cachedChanges = repositoryId
				? getCachedWorkingTreeChanges(repositoryId)
				: null;
			previousRepositoryIdRef.current = repositoryId;
			reloadRequestRef.current = null;
			setState(
				cachedChanges
					? { status: "loaded", changes: cachedChanges }
					: { status: "idle", changes: null },
			);
			setIsDirty(false);
			setIsReloading(false);
			setSummaryCount(cachedChanges?.totalCount ?? 0);
			setHasSummaryLoaded(cachedChanges !== null);
			hasFreshChangesRef.current = cachedChanges !== null;
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
					setCachedWorkingTreeChanges(repositoryId, changes);
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
				invalidateCachedWorkingTreeChanges(repositoryId);
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

	const reload = () => {
		if (!repositoryId) {
			return Promise.resolve();
		}

		const existing = reloadRequestRef.current;
		if (existing?.repositoryId === repositoryId) {
			return existing.promise;
		}

		setIsReloading(true);
		const requestedRepositoryId = repositoryId;
		const promise = loadWorkingTreeChanges(requestedRepositoryId)
			.then((changes) => {
				if (previousRepositoryIdRef.current !== requestedRepositoryId) return;
				setState({ status: "loaded", changes });
				setSummaryCount(changes.totalCount);
				cacheCompleteWorkingTreeSummary(
					requestedRepositoryId,
					changes.totalCount,
				);
				setCachedWorkingTreeChanges(requestedRepositoryId, changes);
				setIsDirty(false);
				setHasSummaryLoaded(true);
				hasFreshChangesRef.current = true;
			})
			.catch((error: unknown) => {
				if (previousRepositoryIdRef.current === requestedRepositoryId) {
					setState((current) => ({
						status: "error",
						changes: current.changes,
						message:
							error instanceof Error
								? error.message
								: "Failed to load working changes.",
					}));
				}
				throw error;
			})
			.finally(() => {
				if (reloadRequestRef.current?.promise === promise) {
					reloadRequestRef.current = null;
					setIsReloading(false);
				}
			});
		reloadRequestRef.current = { promise, repositoryId };
		return promise;
	};

	return {
		...state,
		isDirty,
		isReloading,
		isSummaryLoaded: hasSummaryLoaded,
		totalCount: state.changes?.totalCount ?? summaryCount,
		reload,
	};
}

type ReloadRequest = {
	promise: Promise<void>;
	repositoryId: string;
};
