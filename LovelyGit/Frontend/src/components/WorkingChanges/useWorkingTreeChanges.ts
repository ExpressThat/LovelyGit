import { useEffect, useRef, useState } from "react";
import { subscribeToServerEvent } from "@/lib/commands";
import { createWorkingTreeReload } from "./createWorkingTreeReload";
import {
	applyObservedWorkingTreeChanges,
	countObservedNewPaths,
	shouldApplyObservedWorkingTreeChanges,
} from "./OptimisticWorkingTreeChanges";
import { useWorkingTreePreload } from "./useWorkingTreePreload";
import { loadWorkingTreeChanges } from "./WorkingTreeChangesRequests";
import {
	getWorkingTreeLoadErrorMessage,
	type WorkingTreeChangesState,
	type WorkingTreeReloadRequest,
} from "./WorkingTreeChangesState";
import {
	getCachedWorkingTreeChanges,
	getInitialWorkingTreeChangesState,
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
	const [initial] = useState(() =>
		getInitialWorkingTreeChangesState(repositoryId),
	);
	const [state, setState] = useState<WorkingTreeChangesState>(initial.state);
	const [isDirty, setIsDirty] = useState(false);
	const [summaryCount, setSummaryCount] = useState(
		initial.changes?.totalCount ?? 0,
	);
	const [hasSummaryLoaded, setHasSummaryLoaded] = useState(initial.isLoaded);
	const [isReloading, setIsReloading] = useState(false);
	const hasFreshChangesRef = useRef(initial.isLoaded);
	const reloadRequestRef = useRef<WorkingTreeReloadRequest | null>(null);
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
		const load = () => {
			if (isLoading) {
				reloadAgain = true;
				return reloadRequestRef.current?.promise ?? Promise.resolve();
			}

			const existing = reloadRequestRef.current;
			if (existing?.repositoryId === repositoryId) {
				return existing.promise;
			}

			isLoading = true;
			setState((current) =>
				current.changes
					? { status: "loaded", changes: current.changes }
					: { status: "loading", changes: null },
			);
			const promise = loadWorkingTreeChanges(repositoryId, (changes) => {
				if (isActive) setState({ status: "loaded", changes });
			})
				.then((changes) => {
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
				})
				.catch((error: unknown) => {
					if (isActive) {
						setState((current) => ({
							status: "error",
							changes: current.changes,
							message: getWorkingTreeLoadErrorMessage(error),
						}));
					}
					throw error;
				})
				.finally(() => {
					isLoading = false;
					if (reloadRequestRef.current?.promise === promise) {
						reloadRequestRef.current = null;
					}
					if (isActive && reloadAgain) {
						reloadAgain = false;
						void load().catch(() => undefined);
					}
				});
			reloadRequestRef.current = { promise, repositoryId };
			return promise;
		};

		if (!hasFreshChangesRef.current) void load().catch(() => undefined);
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
					? window.setTimeout(() => void load().catch(() => undefined), 500)
					: window.setTimeout(() => void load().catch(() => undefined), 0);
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

	const reload = createWorkingTreeReload({
		hasFreshChangesRef,
		previousRepositoryIdRef,
		reloadRequestRef,
		repositoryId,
		setHasSummaryLoaded,
		setIsDirty,
		setIsReloading,
		setState,
		setSummaryCount,
	});

	return {
		...state,
		isDirty,
		isReloading,
		isSummaryLoaded: hasSummaryLoaded,
		totalCount: state.changes?.totalCount ?? summaryCount,
		reload,
	};
}
